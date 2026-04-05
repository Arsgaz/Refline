namespace Refline.Api.Enums;

public enum ActivityCategory
{
    Unknown = 0,
    Work = 1,
    Communication = 2,
    ConditionalWork = 3,
    Entertainment = 4,
    System = 5,
    Browser = Communication,
    Meeting = ConditionalWork,
    Break = Entertainment,
    Other = System
}
