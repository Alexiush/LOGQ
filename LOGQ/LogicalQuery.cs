using System;
using System.Collections.Generic;
using static LOGQ.Extensions.ExtensionsMethods;

namespace LOGQ
{
    // BindKey must be connected to some FactVariable or can act like this
    public class BindKey
    {
        public readonly string name;
        public string Value { get; private set; }

        protected BindKey() {}

        public BindKey(string name)
        {
            this.name = name;
        }

        public BindKey(string name, string value)
        {
            this.name = name;
            Value = value;
        }

        public void UpdateValue(Dictionary<BindKey, string> copyStorage, string value)
        {
            if (!copyStorage.ContainsKey(this))
            {
                copyStorage[this] = this.Value;
            }

            Value = value;
        }

        public virtual FactVariable AsFactVariable()
        {
            return new FactVariable(Value);
        }
    }

    public class DummyBound: BindKey
    {
        public DummyBound() {}

        public override FactVariable AsFactVariable()
        {
            return Any();
        }
    }

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
        public Dictionary<BindKey, string> boundsCopy = new Dictionary<BindKey, string>();

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

                public bool isHidden = false;

                public Node() { }

                public Node(LAction boundAction, Node parent)
                {
                    this.boundAction = boundAction;
                    this.parent = parent;
                }
            }

            private Node currentNode;
            private Node root;

            public bool Cut()
            {
                // Rollback results must combine all replaced layers
                // They must be traversed and merged in one big rollback
                // It must provide some interesting way to do it as rollback is driven by the action
                Node pointer = currentNode.parent;
                while (pointer != root)
                {
                    pointer.isHidden = true;
                    pointer = pointer.parent;
                }

                currentNode.parent = pointer;
                return true;
            }

            public void Add(LAction action)
            {
                Node newNode = new Node(action, currentNode);

                if (root is null)
                {
                    root = newNode;
                    currentNode = root;
                    return;
                }
                else
                {
                    currentNode.nextOnTrue = newNode;
                    currentNode = newNode;
                }
            }

            public void AddFalse(LAction action)
            {
                Node newNode = new Node(action, root);
                root.nextOnFalse = newNode;
                
                currentNode = newNode;
            }

            public bool Execute()
            {
                currentNode = root;

                while (!(currentNode == null))
                {
                    if (!currentNode.isHidden)
                    {
                        if (currentNode.boundAction.GetNext())
                        {
                            currentNode = currentNode.nextOnTrue;
                            continue;
                        }

                        // The trick is to assign it to the highest element

                        if (currentNode.nextOnFalse != null)
                        {
                            currentNode = currentNode.nextOnFalse;
                            continue;
                        }
                    }
                    else
                    {
                        currentNode.isHidden = false;
                    }

                    query.ContextRollback(currentNode.boundAction);
                    currentNode = currentNode.parent;
                }

                return !(currentNode == root.parent);
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

        // Maybe always pass knowledge base with fact? 

        private LogicalQuery AddNode<T>(BoundFact<T> fact, KnowledgeBase knowledgeBase, bool pathDirection) where T: new()
        {
            return AddNode(knowledgeBase.CheckForFacts(fact), true);
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

        public LogicalQuery OrWith<T>(BoundFact<T> fact, KnowledgeBase knowledgeBase) where T: new()
        {
            return AddNode(fact, knowledgeBase, false);
        }

        // Scopes out - performs all actions inside as a subquery - inits with context of origin, performs all actions and proceeds
        public LogicalQuery Scope(LogicalQuery innerQuery)
        {
            // as it must be possible to create templated query 
            // both action of creation and action of work must be encapsulated here
            return With(context => innerQuery.Execute());
        }

        public LogicalQuery Cut()
        {
            // adds layer of one action that always succeeds
            // returns new logical query that has context (previously done actions and it's results)
            // but has no info on previous layers (it's cut and will never get back on)

            // so as soon as some root gets succesful and query gets to cut layer - it never goes back

            // TODO: need to add action that restructures tree (selects current node as root and clears it's parent)

            // Must restructure one path - if after cut everything turns out bad - false path must be considered too
            return With(context => tree.Cut());
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

        public bool Execute()
        {
            return tree.Execute();
        }
    }
}
