using Anarchy.Toolkit.Core.Extension;
using Newtonsoft.Json.Serialization;
using System.Text;

namespace Anarchy.Toolkit.Infrastructure.Http.Entity.Http.Common;

/// <summary>
/// <see cref="StringContent"/> holding an object serialized as camel-cased JSON, encoded as UTF-8 with the
/// <c>application/json</c> media type.
/// </summary>
/// <param name="obj">The object to serialize.</param>
public class JsonContent(object obj) : StringContent(obj.ToJson(new CamelCasePropertyNamesContractResolver()), Encoding.UTF8, "application/json");
