using System.Collections.Generic;
using PureSFTP.Models;

namespace PureSFTP.Services;

public interface IConnectionRepository
{
    IReadOnlyList<SavedConnection> GetAll();

    SavedConnection Add(SavedConnection connection);
}
