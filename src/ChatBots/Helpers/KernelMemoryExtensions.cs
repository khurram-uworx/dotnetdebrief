using Microsoft.KernelMemory;
using System;
using System.Threading.Tasks;

namespace Helpers
{
    internal static class KernelMemoryExtensions
    {
        public static async Task MemorizeTextAsync(this IKernelMemory memory, (string, string)[] facts)
        {
            foreach (var fact in facts)
            {
                if (!await memory.IsDocumentReadyAsync(fact.Item1))
                    try
                    {
                        await memory.ImportTextAsync(fact.Item2, documentId: fact.Item1);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed for {fact.Item1} with {ex.Message}");
                    }
            }
        }
    }
}
