---
layout: default
title: Object mapping
parent: Reference
nav_order: 3
---

# Object mapping

### Marker attribute

Marker attribute LOGQ.FactAttribute marks classes that will be mapped to Fact, BoundFact, Rule, BoundRule via source generation.
It has two parameters: 
- factName - suffix for generated fact classes (required)
- mappingMode - which class members will be considered fact members (optional)

Backing fields won`t be mapped in any mode, MappingMode.MarkedData stands for members marked with LOGQ.FactMemberAttribute.

```cs
public enum MappingMode
{
    PublicProperties,
    AllProperties,
    PublicFields,
    AllFields,
    PublicPropertiesAndFields,
    AllPropertiesAndFields,
    MarkedData
}

[System.AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public class FactMemberAttribute : System.Attribute {}

[System.AttributeUsage(System.AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class FactAttribute : System.Attribute
{
    public FactAttribute(string factName, MappingMode mappingMode = MappingMode.PublicProperties)
    {
        FactName = factName;
        MappingMode = mappingMode;
    }

    public string FactName { get; }
    public MappingMode MappingMode { get; }
}
```

### Generated data

Source generator will create classes Fact<i>FactName</i> BoundFact<i>FactName</i>, Rule<i>FactName</i>, BoundRule<i>FactName</i> for each marked class 
in LOGQ.Generation namespace and functions <i>BaseClass</i>.AsFact, <i>BaseClass</i>.AsBoundFact, <i>BaseClass</i>.AsRule, <i>BaseClass</i>.AsBoundRule
in static LOGQ.Generation.FactExtensions.
