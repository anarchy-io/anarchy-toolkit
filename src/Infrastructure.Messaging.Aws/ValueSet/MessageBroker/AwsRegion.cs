using System.ComponentModel;

namespace Anarchy.Toolkit.Infrastructure.Messaging.Aws.ValueSet.MessageBroker;

public enum AwsRegion
{
    [Description("us-east-1")] UsNorthVirginia,
    [Description("us-east-2")] UsOhio,
    [Description("us-west-1")] UsNorthCalifornia,
    [Description("us-west-2")] UsOregon,
    [Description("af-south-1")] AfricaSouthAfricaCapeTown,
    [Description("ap-east-1")] AsiaPacificHongKong,
    [Description("ap-south-2")] AsiaPacificIndiaHyderabad,
    [Description("ap-southeast-3")] AsiaPacificIndonesiaJakarta,
    [Description("ap-southeast-5")] AsiaPacificMalaysia,
    [Description("ap-southeast-4")] AsiaPacificAustraliaMelbourne,
    [Description("ap-south-1")] AsiaPacificIndiaMumbai,
    [Description("ap-southeast-6")] AsiaPacificNewZealand,
    [Description("ap-northeast-3")] AsiaPacificJapanOsaka,
    [Description("ap-northeast-2")] AsiaPacificSouthKoreaSeoul,
    [Description("ap-southeast-1")] AsiaPacificSingapore,
    [Description("ap-southeast-2")] AsiaPacificAustraliaSydney,
    [Description("ap-east-2")] AsiaPacificTaiwanTaipei,
    [Description("ap-southeast-7")] AsiaPacificThailand,
    [Description("ap-northeast-1")] AsiaPacificJapanTokyo,
    [Description("ca-central-1")] CanadaCentral,
    [Description("ca-west-1")] CanadaCalgary,
    [Description("eu-central-1")] EuropeGermanyFrankfurt,
    [Description("eu-west-1")] EuropeIreland,
    [Description("eu-west-2")] EuropeUnitedKingdomLondon,
    [Description("eu-south-1")] EuropeItalyMilan,
    [Description("eu-west-3")] EuropeFranceParis,
    [Description("eu-south-2")] EuropeSpain,
    [Description("eu-north-1")] EuropeSwedenStockholm,
    [Description("eu-central-2")] EuropeSwitzerlandZurich,
    [Description("il-central-1")] IsraelTelAviv,
    [Description("mx-central-1")] MexicoCentral,
    [Description("me-south-1")] MiddleEastBahrainBahrain,
    [Description("me-central-1")] MiddleEastUnitedArabEmirates,
    [Description("sa-east-1")] SouthAmericaBrazilSaoPaulo
}
