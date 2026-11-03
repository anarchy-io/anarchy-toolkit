using Anarchy.Toolkit.Core.Extension;
using Newtonsoft.Json.Serialization;
using System.Text;

namespace Anarchy.Toolkit.Infrastructure.Http.Entity.Http.Common;

public class JsonContent(object obj) : StringContent(obj.ToJson(new CamelCasePropertyNamesContractResolver()), Encoding.UTF8, "application/json");
