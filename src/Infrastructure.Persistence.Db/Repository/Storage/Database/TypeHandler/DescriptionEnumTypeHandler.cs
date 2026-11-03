using Anarchy.Toolkit.Core.Exceptions;
using Anarchy.Toolkit.Core.Extension;
using Dapper;
using System.Data;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Repository.Storage.Database.TypeHandler;

/// <summary>
/// A Dapper type handler that maps enum values to/from their <see cref="System.ComponentModel.DescriptionAttribute"/> string representation.
/// </summary>
public class DescriptionEnumTypeHandler<TEnum> : SqlMapper.TypeHandler<TEnum> where TEnum : Enum
{
    public override void SetValue(IDbDataParameter parameter, TEnum? value)
    {
        parameter.Value = value is not null ? value.ToDescription() : DBNull.Value;
        parameter.DbType = DbType.String;
    }

    public override TEnum Parse(object value)
    {
        return value is string description
            ? description.ToEnumValue<TEnum>()
            : throw new InvalidFormatException(value, typeof(TEnum));
    }
}
