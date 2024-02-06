namespace Felis.Core;

public interface IConsume<in T> 
{
	public abstract void Process(T entity);
}
