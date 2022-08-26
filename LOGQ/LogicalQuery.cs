using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace LOGQ
{
    public class LogicalAction
    {
        // To get rid of single variant / multiple variants system using queue for both
        
        // Queues look too large for pretty big data sets, they rather must be moved to lazy queues
        // or for delegate producing some result type output

        // Better switch to some kind of inner Get Next:
        //  - Search for rules is not linear
        //  - Lists are just pointers to list instantiated elsewhere
        //  - Passing list of actions still requires lots of code, creating GetNext function won't be much harder

        protected BacktrackIterator _iterator;

        internal LogicalAction() { }

        internal LogicalAction(BacktrackIterator iterator)
        {
            this._iterator = iterator;
        }

        internal LogicalAction(List<Predicate<List<IBound>>> actionInitializer)
        {
            int offset = 0;

            _iterator = new BacktrackIterator(
                () =>
                {
                    if (offset >= actionInitializer.Count)
                    {
                        return null;
                    }

                    Predicate<List<IBound>> predicate =
                        actionInitializer[offset];

                    offset++;
                    return predicate;
                },
                () => offset = 0
            ); 
        }

        // Each changed bound variable saves it's state before the change happens
        // at rollback or values are restored

        // Now Bounds themselves control their values history
        // So it must be replaced with a hashset or list (multiple changes)
        public List<IBound> _boundsCopy = new List<IBound>();

        // That means Reset bounds calls for Rollback for each bound in the list
        protected void ResetBounds()
        {
            foreach (IBound boundCopy in _boundsCopy)
            {
                boundCopy.Rollback();
            }
            _boundsCopy.Clear();
        }

        // Rollback must repopulate LAction with values

        public void Rollback()
        {
            ResetBounds();
            _iterator.Reset();
        }

        public virtual bool GetNext()
        {
            Predicate<List<IBound>> predicate = _iterator.GetNext(); 

            while (predicate != null)
            {
                if (predicate.Invoke(_boundsCopy))
                {
                    // Bounds for good approach are saved
                    return true;
                }

                // Bounds for bad approach are turned back
                ResetBounds();
                predicate = _iterator.GetNext();
            }

            return false;
        }
    }

    // Must be better to connect it to the knowledge base from the very beginning
    // Cause it's better to compose the results of queries running on multiple sources data then merging it together 

    // Logical query contains all actions
    // Fluent-style query functions used to update query

    // That's said query is a lazy object
    // As it's unknown when it's build stops query will have an execute method

    // Must be possible to generate "templated query" by providing bounds to it
    // To create a templated query it must accept some sort of bindkeys set passed in
    // It's possible to utilize factory method approach for custom queries, but there are also queries that require fact passing
    // Maybe it can be solved with duck-typing approach
    public class LogicalQuery
    {
        // Possibly need to add tree builder (As it's available only in this class it's pretty encapsulated)
        // Tree might not be modified from outside by means of Add (Cut is allowed)
        // So it can be rewritten with two states

        class QueryTree
        {
            // to pass context
            LogicalQuery query;

            public QueryTree(LogicalQuery query)
            {
                this.query = query;
            }

            public class Node
            {
                public Node parent;

                public LogicalAction boundAction;

                public Node nextOnTrue;
                public Node nextOnFalse;
                public Node localRoot;

                public bool isHidden = false;
                public bool wentFalse = false;

                public Node() { }

                public Node(LogicalAction boundAction, Node parent, Node localRoot)
                {
                    this.boundAction = boundAction;
                    this.parent = parent;
                    this.localRoot = localRoot;
                }
            }

            private Node currentNode = null;
            private Node rootGlobal = null;
            private Node rootLocal = null;

            private Node stateNode = null;

            public bool Cut()
            {
                Node pointer = currentNode.parent;

                while (pointer != currentNode.localRoot)
                {
                    pointer.isHidden = true;
                    pointer = pointer.parent;
                }

                currentNode.parent = pointer;
                return true;
            }

            void SetGlobalRoot(Node newGlobalRoot)
            {
                rootGlobal = newGlobalRoot;
                SetLocalRoot(newGlobalRoot);
                currentNode = newGlobalRoot;
            }

            void SetLocalRoot(Node newLocalRoot)
            {
                rootLocal = newLocalRoot;
            }

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

            public void AddFalse(LogicalAction action)
            {
                Node newNode = new Node(action, rootLocal, rootLocal);

                if (rootLocal is null)
                {
                    // throw exception 
                }

                rootLocal.nextOnFalse = newNode;
                SetLocalRoot(newNode);
                currentNode = newNode;
            }

            public bool Execute()
            {
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

                        if (stateNode.nextOnFalse != null && !stateNode.wentFalse)
                        {
                            stateNode.wentFalse = true;
                            prevNode = stateNode;
                            stateNode = stateNode.nextOnFalse;
                            continue;
                        }
                    }

                    if (stateNode == rootGlobal)
                    {
                        return false;
                    }

                    query.ContextRollback(stateNode.boundAction);
                    stateNode = stateNode.parent;
                }

                stateNode = prevNode;
                return true;
            }

            private void ResetNode(Node node)
            {
                if (node == null)
                {
                    return;
                }

                node.boundAction.Rollback();
                node.isHidden = false;
                node.wentFalse = false;

                ResetNode(node.nextOnTrue);
                ResetNode(node.nextOnFalse);
            }

            public void Reset()
            {
                stateNode = null;
                ResetNode(rootGlobal);
            }
        }

        private QueryTree _tree;
        private bool _finishedBuilding = false;

        public LogicalQuery() 
        {
            _tree = new QueryTree(this);
        }

        public LogicalQuery End()
        {
            _finishedBuilding = true;

            return this;
        }

        private void CheckIfCanBuild()
        {
            if (_finishedBuilding)
            {
                throw new InvalidOperationException("Can't modify query that was built already");
            }
        }

        private LogicalQuery AddNode(LogicalAction action, bool pathDirection)
        {
            CheckIfCanBuild();

            if (pathDirection)
            {
                _tree.Add(action);
            }
            else
            {
                _tree.AddFalse(action);
            }
            return this;
        }

        private LogicalQuery AddNode(BacktrackIterator iterator, bool pathDirection)
        {
            return AddNode(new LogicalAction(iterator), pathDirection);
        }

        private LogicalQuery AddNode(List<Predicate<List<IBound>>> actionInitializer, bool pathDirection)
        {
            return AddNode(new LogicalAction(actionInitializer), pathDirection);
        }

        private LogicalQuery AddNode(Predicate<List<IBound>> actionInitializer, bool pathDirection)
        {
            List<Predicate<List<IBound>>> list =
                new List<Predicate<List<IBound>>>(new[] { actionInitializer });

            return AddNode(list, pathDirection);
        }

        private LogicalQuery AddNode(BoundRule rule, KnowledgeBase knowledgeBase, bool pathDirection)
        {
            return AddNode(knowledgeBase.CheckForRules(rule), pathDirection);
        }

        private LogicalQuery AddNode(BoundFact fact, KnowledgeBase knowledgeBase, bool pathDirection)
        {
            return AddNode(knowledgeBase.CheckForFacts(fact), pathDirection);
        }

        public LogicalQuery With(LogicalAction action)
        {
            return AddNode(action, true);
        }

        public LogicalQuery With(BacktrackIterator iterator)
        { 
            return AddNode(iterator, true);
        }

        public LogicalQuery With(List<Predicate<List<IBound>>> actionInitializer)
        {
            return AddNode(actionInitializer, true);
        }

        public LogicalQuery With(Predicate<List<IBound>> actionInitializer)
        {
            return AddNode(actionInitializer, true);
        }

        public LogicalQuery With(BoundRule rule, KnowledgeBase knowledgeBase)
        {
            return AddNode(rule, knowledgeBase, true);
        }

        public LogicalQuery With(BoundFact fact, KnowledgeBase knowledgeBase)
        {
            return AddNode(fact, knowledgeBase, true);
        }

        // Adds false path
        // Must be some way to restrict multiple OrWith in the same scope as it is just erroneous behaviour
        public LogicalQuery OrWith(LogicalAction action)
        {
            return AddNode(action, false);
        }

        public LogicalQuery OrWith(BacktrackIterator iterator)
        {
            return AddNode(iterator, false);
        }

        public LogicalQuery OrWith(List<Predicate<List<IBound>>> actionInitializer)
        {
            return AddNode(actionInitializer, false);
        }

        public LogicalQuery OrWith(Predicate<List<IBound>> actionInitializer)
        {
            return AddNode(actionInitializer, false);
        }

        public LogicalQuery OrWith(BoundRule rule, KnowledgeBase knowledgeBase)
        {
            return AddNode(rule, knowledgeBase, false);
        }

        public LogicalQuery OrWith(BoundFact fact, KnowledgeBase knowledgeBase)
        {
            return AddNode(fact, knowledgeBase, false);
        }

        // Scopes out - performs all actions inside as a subquery - inits with context of origin, performs all actions and proceeds
        public LogicalQuery WithScoped(LogicalQuery innerQuery)
        {
            CheckIfCanBuild();

            // as it must be possible to create templated query 
            // both action of creation and action of work must be encapsulated here
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

        public LogicalQuery Cut()
        {
            CheckIfCanBuild();

            // adds layer of one action that always succeeds
            // returns new logical query that has context (previously done actions and it's results)
            // but has no info on previous layers (it's cut and will never get back on)

            // so as soon as some root gets succesful and query gets to cut layer - it never goes back
            // Must restructure one path - if after cut everything turns out bad - false path must be considered too

            bool madeCut = false;
            return With(copyStorage =>  madeCut ? madeCut : _tree.Cut());
        }

        public LogicalQuery Fail()
        {
            CheckIfCanBuild();

            return With(copyStorage => false);
        }

        private void ContextRollback(LogicalAction action)
        {
            action.Rollback();
        }

        public void Reset()
        {
            _tree.Reset();
        }

        public bool Execute()
        {
            End();
            return _tree.Execute();
        }
    }
}
