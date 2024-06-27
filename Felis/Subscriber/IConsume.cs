namespace Felis.Subscriber;

public interface IConsume<in T> 
{
	public void Process(T entity);
}
