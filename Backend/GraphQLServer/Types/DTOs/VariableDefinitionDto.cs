using FHOOE.Freydis.Domain.Entities.Variables;
using DomainValueType = FHOOE.Freydis.Domain.Entities.Common.ValueType;

namespace FHOOE.Freydis.GraphQLServer.Types.DTOs;

/// <summary>
///     Data Transfer Object for VariableDefinition.
///     Represents a design-time variable declaration in a procedure.
/// </summary>
/// <param name="Name">Name of the variable.</param>
/// <param name="Type">Type descriptor for this variable.</param>
/// <param name="DefaultValue">Default value for the variable.</param>
/// <param name="Scope">Scope level where the variable is accessible.</param>
/// <param name="Source">Source of the variable's value.</param>
/// <param name="Description">Description of the variable's purpose.</param>
/// <param name="IsReadOnly">Whether the variable is read-only.</param>
public record VariableDefinitionDto(
    string Name,
    DomainValueType Type,
    object? DefaultValue,
    VariableScope Scope,
    VariableSource Source,
    string? Description,
    bool IsReadOnly);