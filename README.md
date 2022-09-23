# LOGQ

LOGQ stands for LOGical Query - it's logical programming tool that works similar to Prolog program. 
It's goal to simplify and extend Prolog features and provide easy integration with mapping tool.

[Documentation](https://alexiush.github.io/LOGQ/)  
[NuGet package](https://www.nuget.org/packages/LOGQ)

## Usage
### Logical query
Main feature is creating logical queries. They're built as trees with nodes represented by logical actions.
Logical actions can be added using With method, some predefined logical actions can be added using Cut, Fail, Succed, WithScoped methods.
```csharp
LogicalQuery query = new LogicalQuery()
  .With(() => true) 
  .Cut()
  .Fail()
  .OrWith(() => 4 == 8/2)
  .Succed();
```

### Logical action
Logical actions are abstractions used for query nodes, they define some sort of condition 
and created by specifying a backtrack iterator that provides them with predicates to check.
```csharp
LogicalQuery query = new LogicalQuery()
  .With(copyStorage => 3 > 1);
  
// In this case predicate will be converted to list with one predicate
// Then it will be converted to backtrack iterator which will iterate through that list
```
Logical actions can be created by passing bactrack iterator, action, predicate or collection of predicates to the logical query using With.
If some of the given predicates is successful - logical action returns true, if each predicate fails - action returns false.

### Backtrack iterator
Backtrack iterator is used to replace Prolog's predicate types with single abstraction.
It consists of generator function and reset action:
- Generator generates predicates for logical actions until it meets some terminal condition after which it returns null that means it's out of predicates.
- Reset action returns generator back to it's initial state.

```csharp
// Backtrack iterator constructor for previous example

internal BacktrackIterator(ICollection<Predicate<List<IBound>>> initializer)
{
    bool enumeratorIsUpToDate = false;
    var enumerator = initializer.GetEnumerator();

    _generator = () =>
    {
        if (!enumeratorIsUpToDate)
        {
            enumerator = initializer.GetEnumerator();
            enumeratorIsUpToDate = true;
        }

        if (!enumerator.MoveNext())
        {
            return null;
        }

        Predicate<List<IBound>> predicate = enumerator.Current;
        return predicate;
    };

    _reset = () => enumeratorIsUpToDate = false;
}
```

### Knowledge base
Knowledge bases are storing facts and rules and can provide backtrack iterators that would do fact-checking or rule-checking on base contents.

```csharp
// Let's say we have some records about school students
KnowledgeBase students = new KnowledgeBase();

// There is a record about student named Andrew in 7th grade
students.DeclareFact(new FactStudent("Andrew", 7))

// Also we know that if we have a record about student in higher grade we also have records about that student in lesser grades
students.DeclareRule(new RuleWithBody(
    new RuleStudent(new AnyValue<string>(), new AnyValue<int>()),
    bound => {
        var boundRule = bound as BoundRuleStudent;

        if (boundRule is null)
        {
            return new LogicalQuery().Fail();
        }

        // Check if there are record in current grade
        // If there is no record check higher grades up to 12th
        return new LogicalQuery()
          .With(new BoundFactStudent(boundRule.Name, boundRule.Grade), students)
          .OrWith(() => boundRule.Grade.Value <= 12)
          .With(new BoundRuleStudent(boundRule.Name, boundRule.Grade.Value + 1), students);
    }));
```

### Facts 
Facts are plain data stored in knowledge base.
```csharp
LogicalQuery query = new LogicalQuery()
  .With(new BoundFactStudent("Andrew", 1), students)
  .With(new BoundFactStudent("Alex", new UnboundVariable<int>()), students)
```
To make a fact-check bound facts are created. Bound facts are facts using bound variables, 
that stands for variables which mutation is controlled by backtracking (their values are restored to preaction state on the way back).

### Rules
Rules are data with associated logical query which returns true only if that data exists.
They made with two parts:
- Rule head that consists of patterns to check if this rule applies
- Rule body that consists of query
```csharp
LogicalQuery query = new LogicalQuery()
  .With(new BoundRuleStudent("Alex", 3), students);
```
To make a rule-check bound rules are created. Like bound facts that's rule heads made of bound variables.

### Objects mapping
Objects are mapped by marking them with Fact attribute.
Marked objects are processed by source generator that creates their Fact, BoundFact, Rule and BoundRule analogues 
and adds static method to convert objects to their representations
```csharp
// Marking student class used in previous examples
// That will generate FactStudent, BoundFactStudent, RuleStudent and BoundRuleStudentClasses 

[LOGQ.Fact("Student")]
public class Student
{
    public string Name { get; set; }
    public int Grade { get; set; }

    public Student(string name, int grade)
    {
        Name = name;
        Grade = grade;
    }
}
```
## Credits
[Andrew Lock's source generator series](https://andrewlock.net/creating-a-source-generator-part-1-creating-an-incremental-source-generator/)
