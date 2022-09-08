---
layout: default
title: Logical querying
parent: Reference
nav_order: 2
---

# Logical querying

> Reference does not provide full listing, but only mentioned parts, for full listing visit [LOGQ GitHub](https://github.com/Alexiush/LOGQ).

### Logical actions and backtrack iterators

Logical actions are nodes in query execution tree. They're iterating through predicates passed by backtrack iterator. 
To instantiate an action backtrack iterator or supported initializer must be passed to one of the [logical query build methods](https://alexiush.github.io/LOGQ/logical_querying_reference.html#build-methods).

Backtrack iterator controls two functions: generator and reset action:
Generator iterates through predicates that can prove the statement associated with backtrack iterator. When there is no more predicates generator must return null.
Reset action modifies generator state, so it starts generating predicates from the very beginning again. 

```cs
public class BacktrackIterator
{
    public BacktrackIterator(Func<Predicate<List<IBound>>> generator, Action reset)
    {
        this._generator = generator;
        this._reset = reset;
    }
    
    public Predicate<List<IBound>> GetNext()
    {
        return _generator.Invoke();
    }

    public void Reset()
    {
        _reset.Invoke();
    }
}
```

### Logical query

Logical query is a group of statements defined through logical actions. Statements are grouped in multiple branches, in each branch statements are chained, 
if it can be proved that some chain consist of true statements - query returns true. Query has build methods to add logical actions and state control methods.

```cs
// Query does not require any initialization parameters
public LogicalQuery() 
{
    _tree = new QueryTree(this);
}
```

### Build methods

Build methods add different logical actions to the query:
- With, that adds logical actions to current branch:
  ```cs
  public LogicalQuery With(LogicalAction action)
  {
      return AddNode(action, true);
  }

  public LogicalQuery With(BacktrackIterator iterator)
  { 
      return AddNode(iterator, true);
  }

  public LogicalQuery With(ICollection<Predicate<List<IBound>>> actionInitializer)
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
  ```
- OrWith that adds logical actions to new branch:
  ```cs
  public LogicalQuery OrWith(LogicalAction action)
  {
      return AddNode(action, false);
  }

  public LogicalQuery OrWith(BacktrackIterator iterator)
  {
      return AddNode(iterator, false);
  }

  public LogicalQuery OrWith(ICollection<Predicate<List<IBound>>> actionInitializer)
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
  ```
- WithScoped that adds action that executes nested logical query:
  ```cs
  public LogicalQuery WithScoped(LogicalQuery innerQuery)
  {
      CheckIfCanBuild();

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
  ```
- Cut that sets current action as a new root of a query and prevents from backtracking further:
  ```cs
  public LogicalQuery Cut()
  {
      CheckIfCanBuild();

      bool madeCut = false;
      return With(copyStorage =>  madeCut ? madeCut : _tree.Cut());
  }
  ```
- Fail and Succeed:
  ```cs
  public LogicalQuery Fail()
  {
      CheckIfCanBuild();

      return With(copyStorage => false);
  }

  public LogicalQuery Succeed()
  {
      CheckIfCanBuild();

      return With(copyStorage => true);
  }
  ```
  
Also there are Not methods, that recieve input similar to With and OrWith and return [negated backtrack iterator](https://alexiush.github.io/LOGQ/logical_querying.html#negate-function).
```cs
namespace LOGQ.Extensions
{
    public static class ExtensionsMethods
    {
        public static LogicalAction Not(ICollection<Predicate<List<IBound>>> actionsToTry)
        {
            return new LogicalAction(actionsToTry
                .Select<Predicate<List<IBound>>, Predicate<List<IBound>>>
                (predicate => context => !predicate(context)).ToList());
        }

        public static LogicalAction Not(Predicate<List<IBound>> actionToTry) 
            => Not(new List<Predicate<List<IBound>>> { actionToTry });

        public static LogicalAction Not(BacktrackIterator iterator)
        {
            return new LogicalAction(iterator.Negate());
        }

        public static LogicalAction Not(BoundFact fact, KnowledgeBase knowledgeBase)
            => Not(knowledgeBase.CheckForFacts(fact));

        public static LogicalAction Not(BoundRule rule, KnowledgeBase knowledgeBase)
            => Not(knowledgeBase.CheckForRules(rule));
    }
}

```

### State control methods

There are three state control methods:
- End that forbids editing the query
  ```cs
  public LogicalQuery End()
  {
      _finishedBuilding = true;

      return this;
  }
  ```
- Execute that forbids editing too and starts query execution
  ```cs
  public bool Execute()
  {
      End();
      return _tree.Execute();
  }
  ```
- Reset that returns query to the initial state (cancels out cuts, branch switches)
  ```cs
  public void Reset()
  {
      _tree.Reset();
  }
  ```
