namespace ArWoh.API.Interface;

public abstract class ILoggerService
{
    public abstract void Success(string msg);
    public abstract void Error(string msg);
    public abstract void Warn(string msg);
    public abstract void Info(string msg);
}