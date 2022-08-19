using System;
using System.Collections.Generic;

namespace LOGQ
{
    public class LAction
    {
        // To get rid of single variant / multiple variants system using queue for both
        
        // Queues look too large for pretty big data sets, they rather must be moved to lazy queues
        // or for delegate producing some result type output

        // Better switch to some kind of inner Get Next:
        //  - Search for rules is not linear
        //  - Lists are just pointers to list instantiated elsewhere
        //  - Passing list of actions still requires lots of code, creating GetNext function won't be much harder

        protected BacktrackIterator iterator;

        protected LAction() { }

        internal LAction(BacktrackIterator iterator)
        {
            this.iterator = iterator;
        }

        internal LAction(List<Predicate<Dictionary<BindKey, string>>> actionInitializer)
        {
            int offset = 0;

            iterator = new BacktrackIterator(
                () =>
                {
                    if (offset >= actionInitializer.Count)
                    {
                        return null;
                    }

                    Predicate<Dictionary<BindKey, string>> predicate =
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
        public Dictionary<BindKey, string> boundsCopy = new Dictionary<BindKey, string>();

        // That means Reset bounds calls for Rollback for each bound in the list
        protected void ResetBounds()
        {
            foreach (KeyValuePair<BindKey, string> boundCopy in boundsCopy)
            {
                boundCopy.Key.UpdateValue(boundsCopy, boundCopy.Value);
            }
        }

        // Rollback must repopulate LAction with values

        public void Rollback()
        {
            ResetBounds();
            iterator.Reset();
        }

        public virtual bool GetNext()
        {
            Predicate<Dictionary<BindKey, string>> predicate = iterator.GetNext(); 

            while (predicate != null)
            {
                if (predicate.Invoke(boundsCopy))
                {
                    // Bounds for good approach are saved
                    return true;
                }

                // Bounds for bad approach are turned back
                ResetBounds();
                predicate = iterator.GetNext();
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

                public LAction boundAction;

                public Node nextOnTrue;
                public Node nextOnFalse;
                public Node localRoot;

                public bool isHidden = false;
                public bool wentFalse = false;

                public Node() { }

                public Node(LAction boundAction, Node parent, Node localRoot)
                {
                    this.boundAction = boundAction;
                    this.parent = parent;
                    this.localRoot = localRoot;
                }
            }

            private Node currentNode = null;
            private Node rootGlobal = null;
            private Node rootLocal = null;

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

            public void Add(LAction action)
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

            public void AddFalse(LAction action)
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
                currentNode = rootGlobal;

                while (!(currentNode == null))
                {
                    if (!currentNode.isHidden)
                    {
                        if (currentNode.boundAction.GetNext())
                        {
                            currentNode = currentNode.nextOnTrue;
                            continue;
                        }

                        if (currentNode.nextOnFalse != null && !currentNode.wentFalse)
                        {
                            currentNode.wentFalse = true;
                            currentNode = currentNode.nextOnFalse;
                            continue;
                        }
                    }

                    if (currentNode == rootGlobal)
                    {
                        return false;
                    }

                    query.ContextRollback(currentNode.boundAction);
                    currentNode = currentNode.parent;
                }

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
                ResetNode(rootGlobal);
            }
        }

        private QueryTree tree;

        // Any templated logical queries are running inside other queries
        // So either templated or just scoped they can utilize this constructor
        public LogicalQuery(LogicalQuery outerScope)
        {
            tree = new QueryTree(this);
        }

        public LogicalQuery() 
        {
            tree = new QueryTree(this);
        }

        private LogicalQuery AddNode(LAction action, bool pathDirection)
        {
            if (pathDirection)
            {
                tree.Add(action);
            }
            else
            {
                tree.AddFalse(action);
            }
            return this;
        }

        private LogicalQuery AddNode(BacktrackIterator iterator, bool pathDirection)
        {
            return AddNode(new LAction(iterator), pathDirection);
        }

        private LogicalQuery AddNode(List<Predicate<Dictionary<BindKey, string>>> actionInitializer, bool pathDirection)
        {
            return AddNode(new LAction(actionInitializer), pathDirection);
        }

        private LogicalQuery AddNode(Predicate<Dictionary<BindKey, string>> actionInitializer, bool pathDirection)
        {
            List<Predicate<Dictionary<BindKey, string>>> list =
                new List<Predicate<Dictionary<BindKey, string>>>(new[] { actionInitializer });

            return AddNode(list, pathDirection);
        }

        private LogicalQuery AddNode<T>(BoundRule<T> rule, KnowledgeBase knowledgeBase, bool pathDirection) where T: new()
        {
            return AddNode(knowledgeBase.CheckForRules(rule), pathDirection);
        }

        private LogicalQuery AddNode<T>(BoundFact<T> fact, KnowledgeBase knowledgeBase, bool pathDirection) where T: new()
        {
            return AddNode(knowledgeBase.CheckForFacts(fact), pathDirection);
        }

        public LogicalQuery With(LAction action)
        {
            return AddNode(action, true);
        }

        public LogicalQuery With(BacktrackIterator iterator)
        {
            return AddNode(iterator, true);
        }

        public LogicalQuery With(List<Predicate<Dictionary<BindKey, string>>> actionInitializer)
        {
            return AddNode(actionInitializer, true);
        }

        public LogicalQuery With(Predicate<Dictionary<BindKey, string>> actionInitializer)
        {
            return AddNode(actionInitializer, true);
        }

        public LogicalQuery With<T>(BoundRule<T> rule, KnowledgeBase knowledgeBase) where T : new()
        {
            return AddNode(rule, knowledgeBase, true);
        }

        public LogicalQuery With<T>(BoundFact<T> fact, KnowledgeBase knowledgeBase) where T: new()
        {
            return AddNode(fact, knowledgeBase, true);
        }

        // Adds false path
        // Must be some way to restrict multiple OrWith in the same scope as it is just erroneous behaviour
        public LogicalQuery OrWith(LAction action)
        {
            return AddNode(action, false);
        }

        public LogicalQuery OrWith(BacktrackIterator iterator)
        {
            return AddNode(iterator, false);
        }

        public LogicalQuery OrWith(List<Predicate<Dictionary<BindKey, string>>> actionInitializer)
        {
            return AddNode(actionInitializer, false);
        }

        public LogicalQuery OrWith(Predicate<Dictionary<BindKey, string>> actionInitializer)
        {
            return AddNode(actionInitializer, false);
        }

        public LogicalQuery OrWith<T>(BoundRule<T> rule, KnowledgeBase knowledgeBase) where T : new()
        {
            return AddNode(rule, knowledgeBase, false);
        }

        public LogicalQuery OrWith<T>(BoundFact<T> fact, KnowledgeBase knowledgeBase) where T: new()
        {
            return AddNode(fact, knowledgeBase, false);
        }

        // Scopes out - performs all actions inside as a subquery - inits with context of origin, performs all actions and proceeds
        public LogicalQuery Scope(LogicalQuery innerQuery)
        {
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
                    return context => !metTerminalCondition;
                },
                () => {
                    innerQuery.Reset();
                    metTerminalCondition = false;
                }
            ));
        }

        public LogicalQuery Cut()
        {
            // adds layer of one action that always succeeds
            // returns new logical query that has context (previously done actions and it's results)
            // but has no info on previous layers (it's cut and will never get back on)

            // so as soon as some root gets succesful and query gets to cut layer - it never goes back
            // Must restructure one path - if after cut everything turns out bad - false path must be considered too
            
            bool madeCut = false;
            return With(context =>  madeCut ? madeCut : tree.Cut());
        }

        public LogicalQuery Fail()
        {
            return With(context => false);
        }

        // Maybe there is a place for not

        private void ContextRollback(LAction action)
        {
            action.Rollback();
        }

        private void Reset()
        {
            tree.Reset();
        }

        public bool Execute()
        {
            return tree.Execute();
        }
    }
}
