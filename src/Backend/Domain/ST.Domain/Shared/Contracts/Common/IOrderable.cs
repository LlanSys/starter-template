using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Domain.Shared.Contracts.Common;

public interface IOrderable
{
    /// <summary>
    /// Controls the sequence in which items appear in dropdowns and lists.
    /// Lower values appear first. Values need not be contiguous — gaps are fine
    /// (e.g. 10, 20, 30) so new items can be inserted without renumbering.
    /// </summary>
    int DisplayOrder { get; set; }
}
