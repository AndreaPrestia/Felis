namespace Felis.Client;

public interface IConsume<in T> 
{
	public void Process(T entity);
}
