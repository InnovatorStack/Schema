# IssuePath

Represent a logical path from the supplied validation root.

## Signature

```csharp
int path.Count
PathSegment path[int index]
string path.ToString()
```

## Example

```csharp
string path = result.Issues[0].Path.ToString();
```

## Behavior

Paths start at $. Segments distinguish properties, indexes, XML attributes and relationship selections. The rendered form quotes names when necessary. Relationship indexes follow the order of matching supplied rows. PathSegment exposes Kind, Name and Index; PathSegmentKind includes Property, Index, Attribute and Relationship.
