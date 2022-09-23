---
layout: default
title: Logical querying
parent: Reference
nav_order: 2
---

# Logical querying

> Reference does not provide full listing, but only mentioned parts, for full listing visit [LOGQ GitHub](https://github.com/Alexiush/LOGQ).

### Logical actions and backtrack iterators

Logical actions are nodes in a query execution tree. They're iterating through predicates passed by backtrack iterator. 
To instantiate an action backtrack iterator or supported initializer must be passed to one of the [logical query build methods](https://alexiush.github.io/LOGQ/logical_querying_reference.html#build-methods).

Backtrack iterator controls two functions: generator and reset action:
The generator iterates through predicates that can prove the statement associated with backtrack iterator. When there are no more predicates, generator must return null.
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
    _builder = new QueryTreeBuilder();
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

  public LogicalQuery With(Func<bool> actionInitializer)
  {
      return AddNode(copyStorage => actionInitializer(), true);
  }

  public LogicalQuery With(Action<List<IBound>> actionInitializer)
  {
      return AddNode(actionInitializer, true);
  }

  public LogicalQuery With(Action actionInitializer)
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

  public LogicalQuery OrWith(Func<bool> actionInitializer)
  {
      return AddNode(copyStorage => actionInitializer(), false);
  }

  public LogicalQuery OrWith(Action<List<IBound>> actionInitializer)
  {
      return AddNode(actionInitializer, false);
  }

  public LogicalQuery OrWith(Action actionInitializer)
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
- WithScoped and OrWithSoped that adds action that executes nested logical query:
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
  ```
- Cut that sets current action as a new root of a query and prevents from backtracking further:
  ```cs
  public LogicalQuery Cut()
  {
      CheckIfCanBuild();

      return With(() =>  _tree.Cut());
  }
  ```
- Fail and Succeed:
  ```cs
  public LogicalQuery Fail()
  {
      CheckIfCanBuild();

      return With(() => false);
  }

  public LogicalQuery Succeed()
  {
      CheckIfCanBuild();

      return With(() => true);
  }
  ```
  
Also, there are Not methods, that recieve input similar to With and OrWith and return [negated backtrack iterator](https://alexiush.github.io/LOGQ/logical_querying.html#negate-function).
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

        public static LogicalAction Not(Func<bool> actionToTry)
            => Not(copyStorage => actionToTry());

        public static LogicalAction Not(Action<List<IBound>> actionToTry)
            => Not(new List<Predicate<List<IBound>>> { copyStorage => { actionToTry(copyStorage); return true; } });

        public static LogicalAction Not(Action actionToTry)
            => Not(new List<Predicate<List<IBound>>> { copyStorage => { actionToTry(); return true; } });

        public static LogicalAction Not(BacktrackIterator iterator)
        {
            return new LogicalAction(iterator.Negate());
        }

        public static LogicalAction Not(BoundFact fact, KnowledgeBase knowledgeBase)
        {
            bool hasConsulted = false;
            BacktrackIterator factsIterator = null;

            BacktrackIterator iterator = new BacktrackIterator(
                () =>
                {
                    if (!hasConsulted)
                    {
                        factsIterator = new BacktrackIterator(knowledgeBase.CheckForFacts(fact));
                        hasConsulted = true;
                    }

                    return factsIterator.GetNext();
                },
                () => hasConsulted = false

            );

            return Not(iterator);
        }

        public static LogicalAction Not(BoundRule rule, KnowledgeBase knowledgeBase)
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
                () => hasConsulted = false
            );

            return Not(iterator);
        }
    }
}

```

### State control methods

There are three state control methods:
- End that forbids editing the query
  ```cs
  public LogicalQuery End()
  {
      if (_tree is null)
      {
          _tree = _builder.Build();
      }

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
