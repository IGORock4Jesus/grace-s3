using Serilog;

namespace GraceS3.Utils;

public class Rollback
{
	private readonly List<IRollbackAction> actions = [];

	public Rollback Add(IRollbackAction action)
	{
		actions.Add(action);

		return this;
	}

	public async Task ExecuteAllAsync()
	{
		Stack<IRollbackAction> executedActions = [];

		try
		{
			foreach (IRollbackAction action in actions)
			{
				await action.Execute();
				executedActions.Push(action);
			}
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Failed to execute rollback actions");
			throw;
		}
		finally
		{
			while (executedActions.Count > 0)
			{
				IRollbackAction action = executedActions.Pop();
				try
				{
					await action.Undo();
				}
				catch (Exception ex)
				{
					Log.Error(ex, "Failed to undo rollback action");
				}
			}
		}
	}
}

public interface IRollbackAction
{
	Task Execute();
	Task Undo();
}

public static class RollbackExtensions
{
	public static Rollback ToRollback(this IEnumerable<IRollbackAction> actions)
	{
		Rollback rollback = new();
		foreach (IRollbackAction action in actions)
		{
			rollback.Add(action);
		}
		return rollback;
	}
}

public sealed class Rollback<TContext>
{
	private readonly List<IRollbackAction<TContext>> actions = [];

	public Rollback<TContext> Add(IRollbackAction<TContext> action)
	{
		actions.Add(action);

		return this;
	}

	public async Task ExecuteAllAsync(TContext context, CancellationToken cancellationToken)
	{
		Stack<IRollbackAction<TContext>> executedActions = [];

		try
		{
			foreach (IRollbackAction<TContext> action in actions)
			{
				await action.Execute(context, cancellationToken);
				executedActions.Push(action);
			}
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Failed to execute rollback actions");

			while (executedActions.Count > 0)
			{
				IRollbackAction<TContext> action = executedActions.Pop();
				try
				{
					await action.Undo(context, cancellationToken);
				}
				catch (Exception undoException)
				{
					Log.Error(undoException, "Failed to undo rollback action");
				}
			}

			throw;
		}
	}
}

public interface IRollbackAction<TContext>
{
	Task Execute(TContext context, CancellationToken cancellationToken);
	Task Undo(TContext context, CancellationToken cancellationToken);
}
