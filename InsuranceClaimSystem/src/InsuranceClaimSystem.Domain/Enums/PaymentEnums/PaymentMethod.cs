public enum PaymentMethod
{
    BankTransfer = 1,    // NEFT/RTGS/IMPS — most common for insurance payouts
    Cheque = 2,          // physical cheque — some older policyholders prefer this
    DigitalWallet = 3    // UPI/Paytm etc — keep this for modern policyholders
}