using Xunit;

// Evita hosts paralelos que se pisan el seeder/DB in-memory
[assembly: CollectionBehavior(DisableTestParallelization = true)]
