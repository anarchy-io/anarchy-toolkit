using System.ComponentModel;

namespace Anarchy.Toolkit.Infrastructure.Messaging.Aws.ValueSet.MessageBroker;

/// <summary>
/// AWS regions, each mapped through its <see cref="DescriptionAttribute"/> to the AWS region code (e.g.
/// <see cref="EuropeIreland"/> is <c>eu-west-1</c>).
/// </summary>
/// <remarks>
/// Use the description value (e.g. via the toolkit's <c>ToDescription()</c> enum extension) when a region code string
/// is needed, such as for <see cref="Anarchy.Toolkit.Infrastructure.Messaging.Aws.Entity.Configuration.Amazon.AwsMessagingOptions.Region"/>.
/// </remarks>
public enum AwsRegion
{
    /// <summary>AWS region <c>us-east-1</c>.</summary>
    [Description("us-east-1")] UsNorthVirginia,
    /// <summary>AWS region <c>us-east-2</c>.</summary>
    [Description("us-east-2")] UsOhio,
    /// <summary>AWS region <c>us-west-1</c>.</summary>
    [Description("us-west-1")] UsNorthCalifornia,
    /// <summary>AWS region <c>us-west-2</c>.</summary>
    [Description("us-west-2")] UsOregon,
    /// <summary>AWS region <c>af-south-1</c>.</summary>
    [Description("af-south-1")] AfricaSouthAfricaCapeTown,
    /// <summary>AWS region <c>ap-east-1</c>.</summary>
    [Description("ap-east-1")] AsiaPacificHongKong,
    /// <summary>AWS region <c>ap-south-2</c>.</summary>
    [Description("ap-south-2")] AsiaPacificIndiaHyderabad,
    /// <summary>AWS region <c>ap-southeast-3</c>.</summary>
    [Description("ap-southeast-3")] AsiaPacificIndonesiaJakarta,
    /// <summary>AWS region <c>ap-southeast-5</c>.</summary>
    [Description("ap-southeast-5")] AsiaPacificMalaysia,
    /// <summary>AWS region <c>ap-southeast-4</c>.</summary>
    [Description("ap-southeast-4")] AsiaPacificAustraliaMelbourne,
    /// <summary>AWS region <c>ap-south-1</c>.</summary>
    [Description("ap-south-1")] AsiaPacificIndiaMumbai,
    /// <summary>AWS region <c>ap-southeast-6</c>.</summary>
    [Description("ap-southeast-6")] AsiaPacificNewZealand,
    /// <summary>AWS region <c>ap-northeast-3</c>.</summary>
    [Description("ap-northeast-3")] AsiaPacificJapanOsaka,
    /// <summary>AWS region <c>ap-northeast-2</c>.</summary>
    [Description("ap-northeast-2")] AsiaPacificSouthKoreaSeoul,
    /// <summary>AWS region <c>ap-southeast-1</c>.</summary>
    [Description("ap-southeast-1")] AsiaPacificSingapore,
    /// <summary>AWS region <c>ap-southeast-2</c>.</summary>
    [Description("ap-southeast-2")] AsiaPacificAustraliaSydney,
    /// <summary>AWS region <c>ap-east-2</c>.</summary>
    [Description("ap-east-2")] AsiaPacificTaiwanTaipei,
    /// <summary>AWS region <c>ap-southeast-7</c>.</summary>
    [Description("ap-southeast-7")] AsiaPacificThailand,
    /// <summary>AWS region <c>ap-northeast-1</c>.</summary>
    [Description("ap-northeast-1")] AsiaPacificJapanTokyo,
    /// <summary>AWS region <c>ca-central-1</c>.</summary>
    [Description("ca-central-1")] CanadaCentral,
    /// <summary>AWS region <c>ca-west-1</c>.</summary>
    [Description("ca-west-1")] CanadaCalgary,
    /// <summary>AWS region <c>eu-central-1</c>.</summary>
    [Description("eu-central-1")] EuropeGermanyFrankfurt,
    /// <summary>AWS region <c>eu-west-1</c>.</summary>
    [Description("eu-west-1")] EuropeIreland,
    /// <summary>AWS region <c>eu-west-2</c>.</summary>
    [Description("eu-west-2")] EuropeUnitedKingdomLondon,
    /// <summary>AWS region <c>eu-south-1</c>.</summary>
    [Description("eu-south-1")] EuropeItalyMilan,
    /// <summary>AWS region <c>eu-west-3</c>.</summary>
    [Description("eu-west-3")] EuropeFranceParis,
    /// <summary>AWS region <c>eu-south-2</c>.</summary>
    [Description("eu-south-2")] EuropeSpain,
    /// <summary>AWS region <c>eu-north-1</c>.</summary>
    [Description("eu-north-1")] EuropeSwedenStockholm,
    /// <summary>AWS region <c>eu-central-2</c>.</summary>
    [Description("eu-central-2")] EuropeSwitzerlandZurich,
    /// <summary>AWS region <c>il-central-1</c>.</summary>
    [Description("il-central-1")] IsraelTelAviv,
    /// <summary>AWS region <c>mx-central-1</c>.</summary>
    [Description("mx-central-1")] MexicoCentral,
    /// <summary>AWS region <c>me-south-1</c>.</summary>
    [Description("me-south-1")] MiddleEastBahrainBahrain,
    /// <summary>AWS region <c>me-central-1</c>.</summary>
    [Description("me-central-1")] MiddleEastUnitedArabEmirates,
    /// <summary>AWS region <c>sa-east-1</c>.</summary>
    [Description("sa-east-1")] SouthAmericaBrazilSaoPaulo
}
