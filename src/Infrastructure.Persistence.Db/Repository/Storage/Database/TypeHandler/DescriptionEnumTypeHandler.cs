using Anarchy.Toolkit.Core.Exceptions;
using Anarchy.Toolkit.Core.Extensions.System;
using Dapper;
using System.Data;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Repository.Storage.Database.TypeHandler;

/// <summary>
/// A Dapper type handler that maps enum values to/from their <see cref="System.ComponentModel.DescriptionAttribute"/> string representation.
/// </summary>
/// <typeparam name="TEnum">The enum type handled.</typeparam>
/// <remarks>
/// Register it once per enum type with Dapper, for example
/// <c>SqlMapper.AddTypeHandler(new DescriptionEnumTypeHandler&lt;OrderStatus&gt;())</c>. The test helpers expose
/// <c>RegisterEnumTypeHandler&lt;TEnum&gt;()</c> for the same purpose.
/// </remarks>
public class DescriptionEnumTypeHandler<TEnum> : SqlMapper.TypeHandler<TEnum> where TEnum : Enum
{
    /// <summary>Writes the enum description as a string parameter, or <see cref="System.DBNull.Value"/> when <paramref name="value"/> is <see langword="null"/>.</summary>
    /// <param name="parameter">The parameter to configure.</param>
    /// <param name="value">The enum value.</param>
    public override void SetValue(IDbDataParameter parameter, TEnum? value)
    {
        parameter.Value = value is not null ? value.ToDescription() : DBNull.Value;
        parameter.DbType = DbType.String;
    }

    /// <summary>Parses a description string read from the database back into the enum value.</summary>
    /// <param name="value">The raw database value.</param>
    /// <returns>The enum value whose description matches <paramref name="value"/>.</returns>
    /// <exception cref="InvalidFormatException">Thrown when <paramref name="value"/> is not a string.</exception>
    public override TEnum Parse(object value)
    {
        return value is string description
            ? description.ToEnumValue<TEnum>()
            : throw new InvalidFormatException(value, typeof(TEnum));
    }
}
