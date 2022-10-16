using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using static LOGQ.DelegateTransformer;

namespace LOGQ
{
    /// <summary>
    /// Class that represents logical action.
    /// It's driven by underlying backtrack iterator and provides interface to
    /// getting and processing predicates passed by iterator and resetting it
    /// Also manages bounds copy storage and rolls bounds back when backtracked
    /// </summary>
    public class LogicalAction
    {
        // To get rid of single variant / multiple variants system using interface that iterates through options
        protected BacktrackIterator _iterator;

        internal LogicalAction() { }

        internal LogicalAction(BacktrackIterator iterator)
        {
            this._iterator = iterator;
        }

        internal LogicalAction(ICollection<Predicate<List<IBound>>> actionInitializer)
        {
            _iterator = new BacktrackIterator(actionInitializer);
        }

        internal LogicalAction(ICollection<Func<bool>> actionInitializer)
        {
            _iterator = new BacktrackIterator(actionInitializer);
        }

        internal LogicalAction(ICollection<Action<List<IBound>>> actionInitializer)
        {
            _iterator = new BacktrackIterator(actionInitializer);
        }

        internal LogicalAction(ICollection<Action> actionInitializer)
        {
            _iterator = new BacktrackIterator(actionInitializer);
        }

        public List<IBound> _boundsCopy = new List<IBound>();

        /// <summary>
        /// Resets all bounds made while applying action
        /// </summary>
        protected void ResetBounds()
        {
            foreach (IBound boundCopy in _boundsCopy)
            {
                boundCopy.Rollback();
            }
            _boundsCopy.Clear();
        }

        /// <summary>
        /// Called when action effect must be reset (while backtracked)
        /// </summary>
        public void Rollback()
        {
            ResetBounds();
            _iterator.Reset();
        }

        /// <summary>
        /// Gets next action until it finds action that returns true or gets out of actions
        /// </summary>
        /// <returns>true if there is a true action, false if there is no more true actions</returns>
        public virtual bool GetNext()
        {
            Predicate<List<IBound>> predicate = _iterator.GetNext();
            bool madeReset = false;

            while (predicate != null)
            {
                if (!madeReset)
                {
                    ResetBounds();
                }

                if (predicate.Invoke(_boundsCopy))
                {
                    // Bounds for good approach are saved
                    madeReset = false;
                    return true;
                }

                // Bounds for bad approach are turned back
                ResetBounds();
                madeReset = true;
                predicate = _iterator.GetNext();
            }

            madeReset = false;
            return false;
        }
    }

    /// <summary>
    /// Class that represents logical query.
    /// 
    /// It uses fluent design - each method returns query, but in another state.
    /// It builds on execution or with End() method.
    /// 
    /// Uses query tree on which performs DFS driven by logical actions.
    /// When logical action is true it proceeds to the next logical action, otherwise it gets backtracked.
    /// If query does not get to the leaf with true it switches to another branch.
    /// If no branch is true query ends with false;
    /// Query can be used multiple times.
    /// </summary>
    public class LogicalQuery
    {
        /// <summary>
        /// Class that represents node(action) in query tree
        /// </summary>
        public class Node
        {
            public Node parent;

            public LogicalAction boundAction;

            public Node nextOnTrue;
            public Node nextOnFalse;
            public Node localRoot;

            public bool isHidden = false;

            public Node() { }

            public Node(LogicalAction boundAction, Node parent, Node localRoot)
            {
                this.boundAction = boundAction;
                this.parent = parent;
                this.localRoot = localRoot;
            }
        }

        /// <summary>
        /// Class that builds query tree
        /// </summary>
        class QueryTreeBuilder
        {
            private Node currentNode = null;
            private Node rootGlobal = null;
            private Node rootLocal = null;

            /// <summary>
            /// Sets global entry point of query
            /// </summary>
            /// <param name="newGlobalRoot">Global entry point</param>
            void SetGlobalRoot(Node newGlobalRoot)
            {
                rootGlobal = newGlobalRoot;
                SetLocalRoot(newGlobalRoot);
                currentNode = newGlobalRoot;
            }

            /// <summary>
            /// Sets entry point for some branch
            /// </summary>
            /// <param name="newLocalRoot">Entry point</param>
            void SetLocalRoot(Node newLocalRoot)
            {
                rootLocal = newLocalRoot;
            }

            /// <summary>
            /// Adds new node to current branch
            /// </summary>
            /// <param name="action">Action to be represented by new node</param>
            public void Add(LogicalAction action)
            {
                Node newNode = new Node(action, currentNode, rootLocal);

                if (rootGlobal is null)
                {
                    SetGlobalRoot(newNode);
                    return;
                }

                currentNode.nextOnTrue = newNode;
                currentNode = newNode;
            }

            /// <summary>
            /// Adds node that moves execution to another branch
            /// </summary>
            /// <param name="action">Action to be represented by new node</param>
            /// <exception cref="InvalidOperationException">
            /// Can't add or branch without main branch
            /// </exception>
            public void AddFalse(LogicalAction action)
            {
                Node newNode = new Node(action, rootLocal, rootLocal);

                if (rootLocal is null)
                {
                    throw new InvalidOperationException("Can't add or branch without main branch");
                }

                rootLocal.nextOnFalse = newNode;
                SetLocalRoot(newNode);
                currentNode = newNode;
            }

            /// <summary>
            /// Builds query tree
            /// </summary>
            /// <returns>Complete query tree</returns>
            public QueryTree Build()
            {
                return new QueryTree(rootGlobal);
            }
        }

        /// <summary>
        /// Class that represents query structure
        /// </summary>
        class QueryTree
        {
            private Node stateNode = null;
            private Node rootGlobal = null;

            /// <summary>
            /// Initializes query tree with it root
            /// </summary>
            /// <param name="rootGlobal">Global root of a tree</param>
            public QueryTree(Node rootGlobal) 
            { 
                this.rootGlobal = rootGlobal;
            }

            /// <summary>
            /// Restructures query tree when cut node is triggered
            /// </summary>
            /// <returns>Success of cut action</returns>
            public bool Cut()
            {
                stateNode.localRoot.isHidden = true;
                stateNode.parent = stateNode.localRoot;
                
                return true;
            }

            /// <summary>
            /// Initiates action rollback
            /// </summary>
            /// <param name="action">Action to be rolled back</param>
            private void ContextRollback(LogicalAction action)
            {
                action.Rollback();
            }

            /// <summary>
            /// Starts query execution - searching for completely true path in query tree
            /// </summary>
            /// <returns>Query execution result</returns>
            public bool Execute()
            {
                if (rootGlobal is null)
                {
                    return true;
                }

                if (stateNode is null)
                {
                    stateNode = rootGlobal;
                }

                Node prevNode = stateNode.parent;

                while (!(stateNode == null))
                {
                    if (!stateNode.isHidden)
                    {
                        if (stateNode.boundAction.GetNext())
                        {
                            prevNode = stateNode;
                            stateNode = stateNode.nextOnTrue;
                            continue;
                        }

                        if (stateNode.nextOnFalse != null)
                        {
                            prevNode = stateNode;
                            stateNode = stateNode.nextOnFalse;
                            Cut();
                            continue;
                        }
                    }

                    if (stateNode == rootGlobal)
                    {
                        return false;
                    }

                    ContextRollback(stateNode.boundAction);
                    stateNode = stateNode.parent;
                }

                stateNode = prevNode;
                return true;
            }

            /// <summary>
            /// Resets state of the node
            /// </summary>
            /// <param name="node">Node to be reset</param>
            private void ResetNode(Node node)
            {
                if (node == null)
                {
                    return;
                }

                node.boundAction.Rollback();
                node.isHidden = false;

                ResetNode(node.nextOnTrue);
                ResetNode(node.nextOnFalse);
            }

            /// <summary>
            /// Resets query state
            /// </summary>
            public void Reset()
            {
                stateNode = null;
                ResetNode(rootGlobal);
            }
        }

        private QueryTreeBuilder _builder;
        private QueryTree _tree;
        private bool _finishedBuilding = false;

        /// <summary>
        /// Creates empty query.
        /// Empty query is always true.
        /// </summary>
        public LogicalQuery() 
        {
            _builder = new QueryTreeBuilder();
        }

        /// <summary>
        /// Marks query as finished to prevent it's mutation
        /// </summary>
        /// <returns></returns>
        public LogicalQuery End()
        {
            if (_tree is null)
            {
                _tree = _builder.Build();
            }

            _finishedBuilding = true;
            return this;
        }

        /// <summary>
        /// Throws exception if complete query is about to be modified
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// When complete query is about to be modified
        /// </exception>
        private void CheckIfCanBuild()
        {
            if (_finishedBuilding)
            {
                throw new InvalidOperationException("Can't modify query that was built already");
            }
        }

        /// <summary>
        /// Adds logical action 
        /// </summary>
        /// <param name="action">Action to be added</param>
        /// <param name="pathDirection">May it be added to the current branch or or branch</param>
        /// <returns>Modified query</returns>
        private LogicalQuery AddNode(LogicalAction action, bool pathDirection)
        {
            CheckIfCanBuild();

            if (pathDirection)
            {
                _builder.Add(action);
            }
            else
            {
                _builder.AddFalse(action);
            }
            return this;
        }

        /// <summary>
        /// Adds logical action based on backtrack iterator
        /// </summary>
        /// <param name="iterator">Iterator used to create an action</param>
        /// <param name="pathDirection">May it be added to the current branch or or branch</param>
        /// <returns>Modified logical query</returns>
        private LogicalQuery AddNode(BacktrackIterator iterator, bool pathDirection)
        {
            return AddNode(new LogicalAction(iterator), pathDirection);
        }

        /// <summary>
        /// Adds logical action based on collection of predicates
        /// </summary>
        /// <param name="actionInitializer">List of predicates used to create an action</param>
        /// <param name="pathDirection">May it be added to the current branch or or branch</param>
        /// <returns>Modified logical query</returns>
        private LogicalQuery AddNode(ICollection<Predicate<List<IBound>>> actionInitializer, bool pathDirection)
        {
            return AddNode(new LogicalAction(actionInitializer), pathDirection);
        }

        /// <summary>
        /// Adds logical action based on collection of predicates without copy storage
        /// </summary>
        /// <param name="actionInitializer">List of predicates used to create an action</param>
        /// <param name="pathDirection">May it be added to the current branch or or branch</param>
        /// <returns>Modified logical query</returns>
        private LogicalQuery AddNode(ICollection<Func<bool>> actionInitializer, bool pathDirection)
        {
            return AddNode(new LogicalAction(actionInitializer), pathDirection);
        }

        /// <summary>
        /// Adds logical action based on collection of actions
        /// </summary>
        /// <param name="actionInitializer">List of predicates used to create an action</param>
        /// <param name="pathDirection">May it be added to the current branch or or branch</param>
        /// <returns>Modified logical query</returns>
        private LogicalQuery AddNode(ICollection<Action<List<IBound>>> actionInitializer, bool pathDirection)
        {
            return AddNode(new LogicalAction(actionInitializer), pathDirection);
        }

        /// <summary>
        /// Adds logical action based on collection of actions without copy storage
        /// </summary>
        /// <param name="actionInitializer">List of predicates used to create an action</param>
        /// <param name="pathDirection">May it be added to the current branch or or branch</param>
        /// <returns>Modified logical query</returns>
        private LogicalQuery AddNode(ICollection<Action> actionInitializer, bool pathDirection)
        {
            return AddNode(new LogicalAction(actionInitializer), pathDirection);
        }

        /// <summary>
        /// Adds logical action based on single predicate
        /// </summary>
        /// <param name="actionInitializer">Predicate used to create an action</param>
        /// <param name="pathDirection">May it be added to the current branch or or branch</param>
        /// <returns>Modified logical query</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private LogicalQuery AddNode(Predicate<List<IBound>> actionInitializer, bool pathDirection)
        {
            List<Predicate<List<IBound>>> list =
                new List<Predicate<List<IBound>>>{ actionInitializer };

            return AddNode(list, pathDirection);
        }

        /// <summary>
        /// Adds logical action based on single predicate without storage
        /// </summary>
        /// <param name="actionInitializer">Predicate used to create an action</param>
        /// <param name="pathDirection">May it be added to the current branch or or branch</param>
        /// <returns>Modified logical query</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private LogicalQuery AddNode(Func<bool> actionInitializer, bool pathDirection)
        {
            return AddNode(actionInitializer.ToPredicate(), pathDirection);
        }

        /// <summary>
        /// Adds logical action based on single action that uses copy storage
        /// </summary>
        /// <param name="actionInitializer">Predicate used to create an action</param>
        /// <param name="pathDirection">May it be added to the current branch or or branch</param>
        /// <returns>Modified logical query</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private LogicalQuery AddNode(Action<List<IBound>> actionInitializer, bool pathDirection)
        {
            return AddNode(actionInitializer.ToPredicate(), pathDirection);
        }

        /// <summary>
        /// Adds logical action based on single action
        /// </summary>
        /// <param name="actionInitializer">Predicate used to create an action</param>
        /// <param name="pathDirection">May it be added to the current branch or or branch</param>
        /// <returns>Modified logical query</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private LogicalQuery AddNode(Action actionInitializer, bool pathDirection)
        {
            return AddNode(actionInitializer.ToPredicate(), pathDirection);
        }

        /// <summary>
        /// Adds action based on rule-check
        /// </summary>
        /// <param name="rule">Rule to be checked</param>
        /// <param name="knowledgeBase">Knowledge base used for rule-check</param>
        /// <param name="pathDirection">May it be added to the current branch or or branch</param>
        /// <returns>Modified logical query</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private LogicalQuery AddNode(BoundRule rule, KnowledgeBase knowledgeBase, bool pathDirection)
        {
            bool hasConsulted = false;
            BacktrackIterator ruleIterator = null;  

            BacktrackIterator iterator = new BacktrackIterator(
                () =>
                {
                    if (!hasConsulted)
                    {
                        ruleIterator = knowledgeBase.CheckForRules(rule);
                        hasConsulted = true;
                    }

                    return ruleIterator.GetNext();
                },
                () =>
                {
                    hasConsulted = false;
                    // ruleIterator.Reset();
                }
            );

            return AddNode(iterator, pathDirection);
        }

        /// <summary>
        /// Adds action based on fact-check
        /// </summary>
        /// <param name="fact">Fact to be checked</param>
        /// <param name="knowledgeBase">Knowledge base used for fact-checking</param>
        /// <param name="pathDirection">May it be added to the current branch or or branch</param>
        /// <returns>Modified logical query</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private LogicalQuery AddNode(BoundFact fact, KnowledgeBase knowledgeBase, bool pathDirection)
        {
            bool hasConsulted = false;
            BacktrackIterator factsIterator = null;

            BacktrackIterator iterator = new BacktrackIterator(
                () =>
                {
                    if (!hasConsulted)
                    {
                        factsIterator = knowledgeBase.CheckForFacts(fact);
                        hasConsulted = true;
                    }

                    return factsIterator.GetNext();
                },
                () =>
                {
                    hasConsulted = false;
                    // factsIterator.Reset();
                }
            );

            return AddNode(iterator, pathDirection);
        }

        /// <summary>
        /// Adds logical action
        /// </summary>
        /// <param name="action">Logical action</param>
        /// <returns>Modified logical query</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LogicalQuery With(LogicalAction action)
        {
            return AddNode(action, true);
        }

        /// <summary>
        /// Adds action based on backtrack iterator
        /// </summary>
        /// <param name="iterator">Iterator used to create an action</param>
        /// <returns>Modified logical query</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LogicalQuery With(BacktrackIterator iterator)
        { 
            return AddNode(iterator, true);
        }

        /// <summary>
        /// Adds action based on collection of predicates
        /// </summary>
        /// <param name="actionInitializer">List of predicates used to create an action</param>
        /// <returns>Modified logical query</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LogicalQuery With(ICollection<Predicate<List<IBound>>> actionInitializer)
        {
            return AddNode(actionInitializer, true);
        }

        /// <summary>
        /// Adds action based on collection of predicates without copy storage
        /// </summary>
        /// <param name="actionInitializer">List of predicates used to create an action</param>
        /// <returns>Modified logical query</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LogicalQuery With(ICollection<Func<bool>> actionInitializer)
        {
            return AddNode(actionInitializer, true);
        }

        /// <summary>
        /// Adds action based on collection of actions
        /// </summary>
        /// <param name="actionInitializer">List of predicates used to create an action</param>
        /// <returns>Modified logical query</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LogicalQuery With(ICollection<Action<List<IBound>>> actionInitializer)
        {
            return AddNode(actionInitializer, true);
        }

        /// <summary>
        /// Adds action based on collection of actions without copy storage
        /// </summary>
        /// <param name="actionInitializer">List of predicates used to create an action</param>
        /// <returns>Modified logical query</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LogicalQuery With(ICollection<Action> actionInitializer)
        {
            return AddNode(actionInitializer, true);
        }

        /// <summary>
        /// Adds action based on predicate
        /// </summary>
        /// <param name="actionInitializer">Predicate used to create an action</param>
        /// <returns>Modified logical query</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LogicalQuery With(Predicate<List<IBound>> actionInitializer)
        {
            return AddNode(actionInitializer, true);
        }

        /// <summary>
        /// Adds action based on predicate without storage
        /// </summary>
        /// <param name="actionInitializer">Predicate used to create an action</param>
        /// <returns>Modified logical query</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LogicalQuery With(Func<bool> actionInitializer)
        {
            return AddNode(actionInitializer, true);
        }

        /// <summary>
        /// Adds action based on action that uses copy storage
        /// </summary>
        /// <param name="actionInitializer">Predicate used to create an action</param>
        /// <returns>Modified logical query</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LogicalQuery With(Action<List<IBound>> actionInitializer)
        {
            return AddNode(actionInitializer, true);
        }

        /// <summary>
        /// Adds action based on action
        /// </summary>
        /// <param name="actionInitializer">Predicate used to create an action</param>
        /// <returns>Modified logical query</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LogicalQuery With(Action actionInitializer)
        {
            return AddNode(actionInitializer, true);
        }

        /// <summary>
        /// Adds action based on rule-checking
        /// </summary>
        /// <param name="rule">Rule to be checked</param>
        /// <param name="knowledgeBase">Knowledge base used for rule-checking</param>
        /// <returns>Modified logical query</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LogicalQuery With(BoundRule rule, KnowledgeBase knowledgeBase) 
        {
            return AddNode(rule, knowledgeBase, true);
        }

        /// <summary>
        /// Adds action based on fact-checking
        /// </summary>
        /// <param name="fact">Fact to be checked</param>
        /// <param name="knowledgeBase">Knowledge base used for fact-checking</param>
        /// <returns>Modified logical query</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LogicalQuery With(BoundFact fact, KnowledgeBase knowledgeBase)
        {
            return AddNode(fact, knowledgeBase, true);
        }

        /// <summary>
        /// Adds logical action to another branch
        /// </summary>
        /// <param name="action">Logical action</param>
        /// <returns>Modified Logical query</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LogicalQuery OrWith(LogicalAction action)
        {
            return AddNode(action, false);
        }

        /// <summary>
        /// Adds action based on backtrack iterator to another branch
        /// </summary>
        /// <param name="iterator">Iterator used to create an action</param>
        /// <returns>Modified logical query</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LogicalQuery OrWith(BacktrackIterator iterator)
        {
            return AddNode(iterator, false);
        }

        /// <summary>
        /// Adds action based on collection of predicates to another branch
        /// </summary>
        /// <param name="actionInitializer">List of predicates used to create an action</param>
        /// <returns>Modified logical query</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LogicalQuery OrWith(ICollection<Predicate<List<IBound>>> actionInitializer)
        {
            return AddNode(actionInitializer, false);
        }

        /// <summary>
        /// Adds action based on collection of predicates without copy storage to another branch
        /// </summary>
        /// <param name="actionInitializer">List of predicates used to create an action</param>
        /// <returns>Modified logical query</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LogicalQuery OrWith(ICollection<Func<bool>> actionInitializer)
        {
            return AddNode(actionInitializer, false);
        }

        /// <summary>
        /// Adds action based on collection of actions to another branch
        /// </summary>
        /// <param name="actionInitializer">List of predicates used to create an action</param>
        /// <returns>Modified logical query</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LogicalQuery OrWith(ICollection<Action<List<IBound>>> actionInitializer)
        {
            return AddNode(actionInitializer, false);
        }

        /// <summary>
        /// Adds action based on collection of actions without copy storage to another branch
        /// </summary>
        /// <param name="actionInitializer">List of predicates used to create an action</param>
        /// <returns>Modified logical query</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LogicalQuery OrWith(ICollection<Action> actionInitializer)
        {
            return AddNode(actionInitializer, false);
        }

        /// <summary>
        /// Adds action based on predicate to another branch
        /// </summary>
        /// <param name="actionInitializer">Predicate used to create an action</param>
        /// <returns>Modified logical query</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LogicalQuery OrWith(Predicate<List<IBound>> actionInitializer)
        {
            return AddNode(actionInitializer, false);
        }

        /// <summary>
        /// Adds action based on predicate without storage to another branch
        /// </summary>
        /// <param name="actionInitializer">Predicate used to create an action</param>
        /// <returns>Modified logical query</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LogicalQuery OrWith(Func<bool> actionInitializer)
        {
            return AddNode(actionInitializer, false);
        }

        /// <summary>
        /// Adds action based on action that uses copy storage to another branch
        /// </summary>
        /// <param name="actionInitializer">Predicate used to create an action</param>
        /// <returns>Modified logical query</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LogicalQuery OrWith(Action<List<IBound>> actionInitializer)
        {
            return AddNode(actionInitializer, false);
        }

        /// <summary>
        /// Adds action based on action to another branch
        /// </summary>
        /// <param name="actionInitializer">Predicate used to create an action</param>
        /// <returns>Modified logical query</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LogicalQuery OrWith(Action actionInitializer)
        {
            return AddNode(actionInitializer, false);
        }

        /// <summary>
        /// Adds action based on rule-checking to another branch
        /// </summary>
        /// <param name="rule">Rule to be checked</param>
        /// <param name="knowledgeBase">Knowledge base used for rule-checking</param>
        /// <returns>Modified logical query</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LogicalQuery OrWith(BoundRule rule, KnowledgeBase knowledgeBase)
        {
            return AddNode(rule, knowledgeBase, false);
        }

        /// <summary>
        /// Adds action based on fact-checking to another branch
        /// </summary>
        /// <param name="fact">Fact to be checked</param>
        /// <param name="knowledgeBase">Knowledge base used for fact-checking</param>
        /// <returns>Modified logical query</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LogicalQuery OrWith(BoundFact fact, KnowledgeBase knowledgeBase)
        {
            return AddNode(fact, knowledgeBase, false);
        }

        /// <summary>
        /// Adds action based on scoped query
        /// </summary>
        /// <param name="innerQuery">Query that will run in the inner scope</param>
        /// <returns>Modified logical query</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LogicalQuery WithScoped(LogicalQuery innerQuery)
        {
            CheckIfCanBuild();

            innerQuery.End();
            bool metTerminalCondition = false;

            return With(new BacktrackIterator(
                () => {
                    if (metTerminalCondition)
                    {
                        return null;
                    }

                    metTerminalCondition = !innerQuery.Execute();
                    return copyStorage => !metTerminalCondition;
                },
                () => {
                    innerQuery.Reset();
                    metTerminalCondition = false;
                }
            ));
        }

        /// <summary>
        /// Adds action based on scoped query to another branch
        /// </summary>
        /// <param name="innerQuery">Query that will run in the inner scope</param>
        /// <returns>Modified logical query</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LogicalQuery OrWithScoped(LogicalQuery innerQuery)
        {
            CheckIfCanBuild();

            innerQuery.End();
            bool metTerminalCondition = false;

            return OrWith(new BacktrackIterator(
                () => {
                    if (metTerminalCondition)
                    {
                        return null;
                    }

                    metTerminalCondition = !innerQuery.Execute();
                    return copyStorage => !metTerminalCondition;
                },
                () => {
                    innerQuery.Reset();
                    metTerminalCondition = false;
                }
            ));
        }


        /// <summary>
        /// Adds layer of one action that always succeeds,
        /// returns new logical query that has context (previously done actions and it's results),
        /// but has no info on previous layers (it's cut and will never get back on),
        /// so as soon as some branch gets succesful and query gets to cut layer - it never goes back
        /// </summary>
        /// <returns>Modified logical query</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LogicalQuery Cut()
        {
            CheckIfCanBuild();

            return With(() =>  _tree.Cut());
        }

        /// <summary>
        /// Adds action that always returns false
        /// </summary>
        /// <returns>Modified logical query</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LogicalQuery Fail()
        {
            CheckIfCanBuild();

            return With(() => false);
        }

        /// <summary>
        /// Adds action that always returns true
        /// </summary>
        /// <returns>Modified logical query</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LogicalQuery Succeed()
        {
            CheckIfCanBuild();

            return With(() => true);
        }

        /// <summary>
        /// Resets query state
        /// </summary>
        public void Reset()
        {
            _tree.Reset();
        }

        /// <summary>
        /// Executes query
        /// </summary>
        /// <returns>Result of execution</returns>
        public bool Execute()
        {
            End();
            return _tree.Execute();
        }
    }
}
