using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ishimotto.Core
{
    /// <summary>
    /// Represent an asynchornus task.
    /// </summary>
    public interface IAsyncTask
    {
        Task ExecuteAsync();
    }
}
