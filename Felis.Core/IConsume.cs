namespace Felis.Core;

public interface IConsume<in T> 
{
	public void Process(T entity);
}
