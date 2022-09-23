---
layout: default
title: Knowledge, facts and rules
parent: Guide
nav_order: 1
---

# Knowledge, facts and rules

In logical programming data about domain presented with facts and rules:  
- Facts are plain data, each fact is represented by a named set of values. To prove that fact is true fact with exact same values is needed.  
- Rules are data described by pattern and proof. To prove that rule is true on some set of values, proof for some matching pattern must be true.

### Facts

Facts are represented by classes inherited from LOGQ.Fact. LOGQ.Fact asks for FactType implementation - origin type of the fact 
(facts can be and most of them are mapped from objects, those defined manually can return own type) and for GetIndexedStorage implementation - object that implements IFactsIndexedStorage interface and wisely searches for fact. Actually, it also needs to have == and != operators, Equals and GetHashCode methods overloaded
as facts must be compared and searched somehow. Plain data that fact contains defined with variables - value containers.

```cs
public class FactStudent : LOGQ.Fact
{
    public Variable<string> Name;
    public Variable<int> Grade;

    public FactStudent(Variable<string> name, Variable<int> grade)
    {
        this.Name = name;
        this.Grade = grade;
    }

    public static bool operator ==(FactStudent fact, FactStudent otherFact)
    {
        return fact.Name.Equals(otherFact.Name) && fact.Grade.Equals(otherFact.Grade);
    }

    public static bool operator !=(FactStudent fact, FactStudent otherFact)
    {
        return !(fact == otherFact);
    }

    public static bool operator ==(FactStudent fact, BoundFactStudent otherFact)
    {
        return fact.Name.Equals(otherFact.Name) && fact.Grade.Equals(otherFact.Grade);
    }

    public static bool operator !=(FactStudent fact, BoundFactStudent otherFact)
    {
        return !(fact == otherFact);
    }

    public override bool Equals(object? obj)
    {
        FactStudent first = obj as FactStudent;

        if (!(first is null))
        {
            return this == first;
        }
        BoundFactStudent second = obj as BoundFactStudent;

        if (!(second is null))
        {
            return this == second;
        }

        return false;
    }

    public override int GetHashCode()
    {
        List<int> propertyCodes = new List<int>
        {
            Name.GetHashCode(),
            Grade.GetHashCode(),
        };
        
        int hash = 19;            

        unchecked
        {
            foreach (var code in propertyCodes)
            {
                hash = hash * 31 + code;
            }
        }

        return hash;
    }

    public override Type FactType()
    {
        return typeof(LOGQ_Examples.ExampleSearchForStudent.Student);
    }

    public override LOGQ.IIndexedFactsStorage IndexedFactsStorage()
    {
        return new IndexedFactStudentStorage();
    }

}
```

Facts used in queries are different. They are called BoundFacts as their variables can be bound to new values during logical query execution. 
When bound fact matches fact, all of its values must be bound to values of the matched fact for what we need to define Bind method.

```cs
public class BoundFactStudent : LOGQ.BoundFact
{
    public BoundVariable<string> Name;
    public BoundVariable<int> Grade;

    public BoundFactStudent(BoundVariable<string> name, BoundVariable<int> grade)
    {
        this.Name = name;
        this.Grade = grade;
    }

    public static bool operator ==(BoundFactStudent fact, BoundFactStudent otherFact)
    {
        return fact.Name.Equals(otherFact.Name) && fact.Grade.Equals(otherFact.Grade);
    }

    public static bool operator !=(BoundFactStudent fact, BoundFactStudent otherFact)
    {
        return !(fact == otherFact);
    }

    public static bool operator ==(BoundFactStudent fact, FactStudent otherFact)
    {
        return fact.Name.Equals(otherFact.Name) && fact.Grade.Equals(otherFact.Grade);
    }

    public static bool operator !=(BoundFactStudent fact, FactStudent otherFact)
    {
        return !(fact == otherFact);
    }

    public override bool Equals(object? obj)
    {
        BoundFactStudent first = obj as BoundFactStudent;

        if (!(first is null))
        {
            return this == first;
        }
        FactStudent second = obj as FactStudent;

        if (!(second is null))
        {
            return this == second;
        }

        return false;
    }

    public override Type FactType()
    {
        return typeof(LOGQ_Examples.ExampleSearchForStudent.Student);
    }

    public override LOGQ.IIndexedFactsStorage IndexedFactsStorage()
    {
        return new IndexedFactStudentStorage();
    }

    public override void Bind(Fact fact, List<IBound> copyStorage)
    {
        if (fact.FactType() != FactType())
        {
            throw new ArgumentException("Can't compare facts based on different types");
        }
        FactStudent typedFact = (FactStudent)fact;
        this.Name.UpdateValue(copyStorage, typedFact.Name.Value);
        this.Grade.UpdateValue(copyStorage, typedFact.Grade.Value);
    }
}
```

### Rules

Rules are defined by RuleWithBody class, instances of which are made with rule pattern (Rule class) and function that receives BoundRule (from query) and returns
logical query that proves or disproves that rule is appropriate. 

```cs
RuleWithBody example = new RuleWithBody(
  // Rule head - pattern
  new RuleStudent(new AnyValue<string>(), new AnyValue<int>()),
  // Rule body - proof
  bound => {
      var boundRule = bound as BoundRuleStudent;

      if (boundRule is null)
      {
          return new LogicalQuery().Fail();
      }

      return new LogicalQuery()
      .With(new BoundFactStudent(boundRule.Name, boundRule.Grade), students)
      .OrWith(() => boundRule.Grade.Value <= 12)
      .With(new BoundRuleStudent(boundRule.Name, boundRule.Grade.Value + 1), students);
  }));
```

Rule class there stands for rule pattern, it's similar to fact, except it does not provide value, but pattern:

```cs
public class RuleStudent : LOGQ.Rule
{
    public RuleVariable<string> Name;
    public RuleVariable<int> Grade;

    public RuleStudent(RuleVariable<string> name, RuleVariable<int> grade)
    {
        this.Name = name;
        this.Grade = grade;
    }

    public static bool operator ==(RuleStudent fact, RuleStudent otherFact)
    {
        return fact.Name.Equals(otherFact.Name) && fact.Grade.Equals(otherFact.Grade);
    }

    public static bool operator !=(RuleStudent fact, RuleStudent otherFact)
    {
        return !(fact == otherFact);
    }

    public static bool operator ==(RuleStudent fact, BoundRuleStudent otherFact)
    {
        return fact.Name.Equals(otherFact.Name) && fact.Grade.Equals(otherFact.Grade);
    }

    public static bool operator !=(RuleStudent fact, BoundRuleStudent otherFact)
    {
        return !(fact == otherFact);
    }

    public override bool Equals(object? obj)
    {
        BoundRuleStudent first = obj as BoundRuleStudent;

        if (!(first is null))
        {
            return this == first;
        }
        RuleStudent second = obj as RuleStudent;

        if (!(second is null))
        {
            return this == second;
        }

        return false;
    }

    public override Type RuleType()
    {
        return typeof(LOGQ_Examples.ExampleSearchForStudent.Student);
    }

    public override LOGQ.IIndexedRulesStorage IndexedRulesStorage()
    {
        return new IndexedRuleStudentStorage();
    }

}
```

And bound rules are even more similar to bound facts than rules to facts, when rules use patterns instead of values bound rules use the same bound variables:

```cs
public class BoundRuleStudent : LOGQ.BoundRule
{
    public BoundVariable<string> Name;
    public BoundVariable<int> Grade;

    public BoundRuleStudent(BoundVariable<string> name, BoundVariable<int> grade)
    {
        this.Name = name;
        this.Grade = grade;
    }

    public static bool operator ==(BoundRuleStudent fact, BoundRuleStudent otherFact)
    {
        return fact.Name.Equals(otherFact.Name) && fact.Grade.Equals(otherFact.Grade);
    }

    public static bool operator !=(BoundRuleStudent fact, BoundRuleStudent otherFact)
    {
        return !(fact == otherFact);
    }

    public static bool operator ==(BoundRuleStudent fact, RuleStudent otherFact)
    {
        return fact.Name.Equals(otherFact.Name) && fact.Grade.Equals(otherFact.Grade);
    }

    public static bool operator !=(BoundRuleStudent fact, RuleStudent otherFact)
    {
        return !(fact == otherFact);
    }

    public override bool Equals(object? obj)
    {
        RuleStudent first = obj as RuleStudent;

        if (!(first is null))
        {
            return this == first;
        }
        BoundRuleStudent second = obj as BoundRuleStudent;

        if (!(second is null))
        {
            return this == second;
        }

        return false;
    }

    public override Type RuleType()
    {
        return typeof(LOGQ_Examples.ExampleSearchForStudent.Student);
    }

    public override LOGQ.IIndexedRulesStorage IndexedRulesStorage()
    {
        return new IndexedRuleStudentStorage();
    }

}
```

### Generic facts (Aliases)

Usually it's better to use tiny types to achieve more accurate code. However, sometimes it does not make that much sense.  
So there are generic facts too - aliases. Aliases code is similar to examples above, but aliases have only one variable that holds object of type T.

```cs
FactAlias<string> stringFact = "s".AsFact(); 
```

FactAliases always considered suitable to be searched with hashcode.

### Variables

There are different types of variables and most of which have been described above:
- Variable<T>: just a value container, needs to have a value
- BoundVariable<T>: value container that journals its value changes, needs to have a value
- UnboundVariable<T>: BoundVariable that is not bound yet, can't have initial value
- RuleVariable<T>: base class for rule patterns, allows own pattern definition:
  - AnyValue<T>: any value is accepted, can't have a value
  - AnyValueBound<T>: any value except unbound variable is accepted, can't have a value
  - Equal<T>: only value that is equal to the pattern value, needs to have a value
  - NotEqual<T>: any value that is not equal to the pattern value, needs to have a value
  - NotEqualBound<T>: any value that is not equal to the pattern value and bound, needs to have a value 
  - UnboundValue<T>: only unbound variables, can't have a value
 
```cs
// Variable usage
FactStudent fact = new FactStudent(new Variable<string>("Alex"), new Variable<int>(1));
// BoundVariable and UnboundVariable usage
BoundFactStudent boundFact = new BoundFactStudent(new BoundVariable<string>("Alex"), new UnboundVariable<int>())
  
// Variables and bound variables can be defined implicitly
fact = new FactStudent("Alex", 1);
boundFact = new BoundFactStudent("Alex", new UnboundVariable<int>());
  
// Rule patterns usage
RuleStudent rule = new RuleStudent(new AnyValue<string>(), new NotEqual<int>(1));
```

### Indexed storages
    
In most cases searching for facts can be speeded up with indexes just like in databases. Standard implementation of indexed storage uses HashSet<Fact> for fact itself and Dictionary<int, Cluster<Fact>> (Cluster is an abstraction on a list of facts, with dictionaries facts are grouped in clusters by some property hashcodes) for each property that can give valid hashcodes.  
    
For rules and cases when hashing can't be applied simpler and slower solution used - lists. 
    
```cs
// Generated storages
    
public class IndexedFactStudentStorage : LOGQ.IIndexedFactsStorage
{
    List<LOGQ.Fact> facts = new List<LOGQ.Fact>();
    HashSet<FactStudent> factSet = new HashSet<FactStudent>();

    Dictionary<int, Cluster<Fact>> Name = new Dictionary<int, Cluster<Fact>>();
    Dictionary<int, Cluster<Fact>> Grade = new Dictionary<int, Cluster<Fact>>();

    public void Add(LOGQ.Fact fact)
    {
        FactStudent factCasted = (FactStudent)fact;

        facts.Add(factCasted);
        factSet.Add(factCasted);

        int NameHash = factCasted.Name.Value.GetHashCode();
        if (!Name.ContainsKey(NameHash))
        {
            Name.Add(NameHash, new Cluster<Fact>());
        } 
        Name[NameHash].Add(fact);

        int GradeHash = factCasted.Grade.Value.GetHashCode();
        if (!Grade.ContainsKey(GradeHash))
        {
            Grade.Add(GradeHash, new Cluster<Fact>());
        } 
        Grade[GradeHash].Add(fact);


    }

    public List<LOGQ.Fact> FilteredBySample(LOGQ.BoundFact sample)
    {
        BoundFactStudent sampleCasted = (BoundFactStudent)sample;
        List<(Cluster<Fact> cluster, int size)> clusters = new List<(Cluster<Fact> cluster, int size)>();

        if (sampleCasted.Name.IsBound())
        {
            int code = sampleCasted.Name.Value.GetHashCode();

            if (Name.ContainsKey(code))
            {
                Cluster<Fact> cluster = Name[code];
                clusters.Add((cluster, cluster.Size));
            }
            else
            {
                clusters.Add((new Cluster<Fact>(), 0));
            }
        }


        if (sampleCasted.Grade.IsBound())
        {
            int code = sampleCasted.Grade.Value.GetHashCode();

            if (Grade.ContainsKey(code))
            {
                Cluster<Fact> cluster = Grade[code];
                clusters.Add((cluster, cluster.Size));
            }
            else
            {
                clusters.Add((new Cluster<Fact>(), 0));
            }
        }

        if (clusters.Count == 2)
        {
            FactStudent factCopy = new FactStudent(
                sampleCasted.Name.Value, 
                sampleCasted.Grade.Value
            );

            if (factSet.Contains(factCopy))
            {
                return new List<LOGQ.Fact> { (LOGQ.Fact)(factCopy) };
            }

        }

        if (clusters.Count == 0)
        {
            return facts.Where(fact => fact.Equals(sample)).ToList();
        }

        return clusters
            .OrderBy(cluster => cluster.size)
            .First()
            .cluster
            .GetValues();
    }

}

public class IndexedRuleStudentStorage : LOGQ.IIndexedRulesStorage
{
    List<LOGQ.RuleWithBody> rules = new List<LOGQ.RuleWithBody>();       


    public void Add(LOGQ.RuleWithBody rule)
    {
        rules.Add(rule);
    }

    public List<LOGQ.RuleWithBody> FilteredByPattern(LOGQ.BoundRule pattern)
    {
        return rules.Where(rule => rule.Head.Equals(pattern)).ToList();
    }
}
```
    
### Objects mapping

LOGQ contains [source generator](https://docs.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/source-generators-overview).
It can automatically map your classes to fact classes: Fact, BoundFact, Rule, BoundRule. It's highly recommended to use as usually 
you would need standard behaviour for your facts, and it's the best way to get it.

Classes to be mapped must be marked with marker attribute LOGQ.Fact. It requires factName - suffix for class names and has optional mapping mode:
PublicProperties(default), AllProperties, PublicFields, AllFields, PublicPropertiesAndFields, AllPropertiesAndFields, MarkedData(members marked by LOGQ.FactMember).
Also, backing fields are not mapped.

```cs
// class mapped to examples above
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

With classes mapped mapper creates mapping functions for objects:

```cs
public static class FactExtensions
{
    public static FactStudent AsFact(this LOGQ_Examples.ExampleSearchForStudent.Student origin)
    {
        return new FactStudent(origin.Name, origin.Grade);
    }

    public static BoundFactStudent AsBoundFact(this LOGQ_Examples.ExampleSearchForStudent.Student origin)
    {
        return new BoundFactStudent(origin.Name, origin.Grade);
    }

    public static RuleStudent AsRule(this LOGQ_Examples.ExampleSearchForStudent.Student origin)
    {
        return new RuleStudent(origin.Name, origin.Grade);
    }

    public static BoundRuleStudent AsBoundRule(this LOGQ_Examples.ExampleSearchForStudent.Student origin)
    {
        return new BoundRuleStudent(origin.Name, origin.Grade);
    }

}
```
    
Generator creates indexed storages for facts and rules too. By default it assumes that it's possible to search for facts faster by hashing the fact and values of each variable. Of course it's not always true, and with NoIndexing and NotHashComparable attributes it can be specified explicitly if fact can't be indexed at all or if it's so for some of it's values.
    
```cs
// Property that contains WeirdlyHashedList can't be indexed 
[LOGQ.Fact("WeirdListWithNumber")]
public class WeirdListWithNumber
{
    [LOGQ.NotHashComparable]
    public WeirdlyHashedList WierdList { get; set; }
    public int Number { get; set; }
}

// Fact that contains only WeirdlyHashedList can't be indexed
[LOGQ.NoIndexing]
[LOGQ.Fact("WeirdList")]
public class JustWeirdList
{
    public WeirdlyHashedList WierdList { get; set; }
}   
    
```

### KnowledgeBase

In LOGQ data is grouped inside knowledge base. It's storing facts and rules and describes how to make fact-checking or rule-checking on it.
```cs
// Knowledge base creation
KnowledgeBase knowledge = new KnowledgeBase();

// Fact declaration
students.DeclareFact(new FactStudent("Andrew", 7));

// Rule declaration
students.DeclareRule(new RuleWithBody(
    new RuleStudent(new AnyValue<string>(), new AnyValue<int>()),
    bound => {
        var boundRule = bound as BoundRuleStudent;

        if (boundRule is null)
        {
            return new LogicalQuery().Fail();
        }

        return new LogicalQuery()
        .With(new BoundFactStudent(boundRule.Name, boundRule.Grade), students)
        .OrWith(() => boundRule.Grade.Value <= 12)
        .With(new BoundRuleStudent(boundRule.Name, boundRule.Grade.Value + 1), students);
    }));
```
