namespace Felis.Client;

public interface IConsume<in T> 
{
	public abstract void Process(T entity);
}
