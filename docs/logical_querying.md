---
layout: default
title: Logical querying
parent: Guide
nav_order: 2
---

# Logical querying

### Logical query

Logical query is a main tool in LOGQ. Logical query builds a tree from chained logical statements (logical actions in LOGQ). 
Logical query result shows if it can reach any leaf of a tree by proving each statement is true from a current state. When query is empty it always returns true.
Otherwise, it uses search with backtracking to prove statements - that means it tries to go as deep in tree as it can before it will run out of proofs, 
if it will state will be turned back to the moment before visiting last node(state reset applies only to [bound variables](https://alexiush.github.io/LOGQ/knowledge.html#variables)):

![pIcTuRe](https://i.imgur.com/QgcnYe8.png)

### Logical action

Logical statements are handled with logical actions. Logical actions are iterating through available proofs defined as predicates.
Logical actions can't be created by user directly, but by query itself. Their working principle is searching for 
applicable predicate until they ran out of options, when they have applicable proof they return true, when they ran out of options - false.
However, on backtrack they're always reset populated with options again.

![pIcTuRe](https://i.imgur.com/VMNP5Qm.png)

### Backtrack iterator

While logical actions can't be created manually backtrack iterators can! Backtrack iterators used in logical actions to check for available proofs 
and can be used to create logical action. Bactrack iterators must contain two parts:
- Generator function: function that iterates through available proofs and returns them or returns null when it ran out of options.
- Reset action: function that changes generator state, so it can start over.

```cs
// Function used to automatically convert collection of predicates (ICollection<Predicate<List<IBound>>> actionInitializer)
// to the backtrack iterator
var enumerator = actionInitializer.GetEnumerator();

iterator = new BacktrackIterator(
    () =>
    {   
        if (!enumerator.MoveNext())
        {
            return null;
        }

        Predicate<List<IBound>> predicate = enumerator.Current;
        return predicate;
    },
    () => enumerator = actionInitializer.GetEnumerator()
); 
```

### Ways to create logical action

Logical actions can be created in many ways:
- With method that accepts as initializers:
  - Single predicate
  - Collection of predicates
  - Backtrack iterator
  - Bound fact + knowledge base to perform fact-check on
  - Bound rule + knowledge base to perform rule-check on 
- WithScoped method that runs some query in scope
- OrWith method that accepts same input as With method and used for branching
- Fail and Succeed methods that predicates that always return false or true respectively
- Cut method that blocks backtracking further then it's node

```cs
LogicalQuery query = new LogicalQuery()
  .With(copyStorage => true) 
  .Cut()
  .Fail()
  .OrWith(copyStorage => 4 == 8/2)
  .Succed();
```

### Negate function

Negate function replaces backtrack iterator with it's negated version - now it needs to check 
if statement can't be proved - all available options must return false:

```cs
LogicalQuery query = new LogicalQuery()
  .With(Not(new BoundFactStudent("Alex", 2), students));
  
// Query will return true only if there is no student Alex in second grade
```

### Branches

Query can have multiple branches(chains of nodes in this context). Another branch is executed after previous branch ran out of options on all layers.
New branch can be declared through OrWith method - with OrWith node creation path from previous OrWith (or root) made as well and used to switch branch
under condition mentioned above.

```cs
BoundVariable<string> name = "Bob";
BoundVariable<int> grade = 4;

LogicalQuery query = new LogicalQuery()
  .With(new BoundFactStudent(name, grade), students)
  .Cut()
  .With(new BoundFactStudent(new UnboundVariable<string>(), grade), students)
  .OrWith(new BoundFactStudent(name, new UnboundVariable<int>()), students);
  
// Checks if there is a student named Bob in 4th grade then checks if there are another students in 4th grade 
// if Bob is pretty lonely we check whether there are other Bobs
```

### Logical query state

Logical query state is pretty unusual:
- Query remembers it's state, when executed again it continues to iterate through proofs - query that returns true on first (2nd, 3rd and so on) iterations
can return false. To reset query Reset method used.
- Query can't be modified after first execution. If you declare your query long before the execution and don't want it to be modified later use End method.

```cs
var name = new UnboundVariable<string>();
var grade = UnboundVariable<int>();

LogicalQuery query = new LogicalQuery()
  .With(new BoundFactStudent(name, grade), students)
  .End(); 
  
// Yes, this example does not make sense at all
int studentsCount = 0;

while(query.Execute())
{
  studentsCount++;
}

// Hmm, how about double students count
query.Reset();

while(query.Execute())
{
  studentsCount++;
}
```
