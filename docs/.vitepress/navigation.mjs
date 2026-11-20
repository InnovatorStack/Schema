export const apiGroups = [
  ['Schemas', ['String', 'Decimal', 'DecimalText', 'Integer', 'IntegerText', 'Boolean',
    'BooleanText', 'Object', 'Array', 'Item']],
  ['Fields and shapes', ['Field', 'Attribute', 'Relationships', 'Property', 'UnknownProperties']],
  ['Rules', ['NotBlank', 'MinLength', 'MaxLength', 'Matches', 'Min', 'Max', 'MinCount',
    'MaxCount', 'Check', 'And']],
  ['Pipelines', ['Optional', 'Nullable', 'RequireValue', 'Transform', 'TransformNullable',
    'WithMessage']],
  ['Validation', ['Parse', 'SafeParse', 'Validate', 'ValidateJson', 'ValidateXml', 'ValidateAml']],
  ['Results and options', ['SafeParseResult', 'ParsedObject', 'ValidationError', 'ValidationException', 'InputFormat', 'ValidationResult', 'ValidatedObject', 'ValidatedArray',
    'ValidationValue', 'ValidationIssue', 'IssuePath', 'IssueCode', 'ValidationOptions']]
];

export function slug(name) {
  return name.replace(/([a-z])([A-Z])/g, '$1-$2').toLowerCase();
}

export const guideLinks = [
  ['Introduction', 'introduction'], ['Installation', 'installation'],
  ['Server Methods', 'server-methods'], ['Define a schema', 'schemas'],
  ['Nested data', 'nested-data'], ['Missing and null', 'presence'],
  ['Pipelines', 'pipelines'], ['Reuse schemas', 'reuse'], ['Handle errors', 'errors'],
  ['Validated output', 'output'], ['Limits and cancellation', 'limits'],
  ['Compatibility', 'compatibility'], ['Performance', 'performance']
];

export const exampleLinks = [
  ['Validate a Part', 'part'], ['Parse an assembly', 'assembly'], ['Validate an order', 'order'],
  ['Validate a Part structure', 'part-structure'], ['Handle errors yourself', 'manual-errors'],
  ['Transform values', 'transform-values']
];
