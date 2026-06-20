namespace Cairnly.API.Models.Entities;

/// <summary>
/// The kind of financial account.
/// </summary>
public enum AccountType
{
    /// <summary>A checking account.</summary>
    Checking,

    /// <summary>A savings account.</summary>
    Savings,

    /// <summary>A taxable brokerage account.</summary>
    Brokerage,

    /// <summary>A retirement account (e.g. 401k, IRA).</summary>
    Retirement,

    /// <summary>A health savings account.</summary>
    Hsa,

    /// <summary>Physical cash.</summary>
    Cash,

    /// <summary>Real estate holdings.</summary>
    RealEstate,

    /// <summary>A vehicle asset.</summary>
    Vehicle,

    /// <summary>A cryptocurrency holding.</summary>
    Crypto,

    /// <summary>A mortgage liability.</summary>
    Mortgage,

    /// <summary>A credit card liability.</summary>
    CreditCard,

    /// <summary>A loan liability.</summary>
    Loan
}
