using MTS.Models;

namespace MTS.Data;

using Microsoft.EntityFrameworkCore.Diagnostics;

public class InventoryMaterializationInterceptor : IMaterializationInterceptor
{
    public object InitializedInstance(MaterializationInterceptionData data, object entity)
    {
        if (entity is InventoryManager inventory)
        {
            inventory.Init();
        }
        return entity;
    }
}