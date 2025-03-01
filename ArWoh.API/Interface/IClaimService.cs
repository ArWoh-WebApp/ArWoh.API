namespace ArWoh.API.Interface;

public interface IClaimService
{
    int GetCurrentUserId();
    string GetCurrentUserRole();
    string GetCurrentUserEmail();
}