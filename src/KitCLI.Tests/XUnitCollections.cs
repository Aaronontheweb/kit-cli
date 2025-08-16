using Xunit;

namespace KitCLI.Tests;

// Define a test collection to prevent parallel execution of tests that use console output
[CollectionDefinition("Console Output Tests", DisableParallelization = true)]
public class ConsoleOutputTestCollection
{
    // This class is never instantiated, it's just used to define the collection
}
