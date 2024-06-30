namespace Felis.Subscriber;

public interface ISubscribe<in T> 
{
    public void Listen(T entity);
}