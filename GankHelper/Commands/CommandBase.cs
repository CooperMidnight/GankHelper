using System.Threading.Tasks;

namespace GankHelper.Commands;

internal abstract class CommandBase
{
    public abstract Task ExecuteAsync();
}
