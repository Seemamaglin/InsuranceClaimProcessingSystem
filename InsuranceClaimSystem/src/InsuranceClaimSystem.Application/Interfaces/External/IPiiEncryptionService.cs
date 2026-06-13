namespace InsuranceClaimSystem.Application.Interfaces.External;                                 
                                                                                                   
   public interface IPiiEncryptionService                                                          
   {                                                                                               
       string Encrypt(string plainText);                                                           
       string Decrypt(string cipherText);                                                          
       string MaskAadhaar(string aadhaar);                                                         
       Task<byte[]> EncryptAsync(byte[] plainData);                                                
       Task<byte[]> DecryptAsync(byte[] cipherData);                                               
   }