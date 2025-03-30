using System;
using GdUnit4;
using Godot;
using System.Collections.Generic;
using static GdUnit4.Assertions;
using OctTreeNamespace;

[TestSuite]
public class OctTreeFuzzTests
{
    private OctTree? _sut;
    private Random _random;
    private const int SEED = 12345; // Fixed seed for reproducibility
    private Vector3I _boundsSize = new Vector3I(10000, 10000, 10000);
    private int _maxDepth = 10;
    private const int NUM_FUZZ_ELEMENTS = 1000;
    
    [Before]
    public void Setup()
    {
        _random = new Random(SEED);
    }
    
    // CRITICAL: Added BeforeTest method to reset OctTree before each test
    [BeforeTest]
    public void SetupTest()
    {
        // Initialize a fresh OctTree before each test to prevent
        // elements from carrying over between tests
        _sut = new OctTree(_boundsSize, _maxDepth);
    }
    
    // Helper method to generate random positions
    private Vector3I RandomPosition(bool useFullRange = true)
    {
        int range = useFullRange ? _boundsSize.X / 2 : 100;
        return new Vector3I(
            (int)((_random.NextDouble() * 2 - 1) * range),
            (int)((_random.NextDouble() * 2 - 1) * range),
            (int)((_random.NextDouble() * 2 - 1) * range)
        );
    }
    
    // Helper method to generate random search box
    private (Vector3I, Vector3I) RandomSearchBox(int size = 100)
    {
        Vector3I center = RandomPosition();
        int halfSize = Math.Max(1, _random.Next(size));
        
        Vector3I min = new Vector3I(
            center.X - halfSize,
            center.Y - halfSize,
            center.Z - halfSize
        );
        
        Vector3I max = new Vector3I(
            center.X + halfSize,
            center.Y + halfSize,
            center.Z + halfSize
        );
        
        return (min, max);
    }
    
    [TestCase]
    public void Test_FuzzAddAndSearch()
    {
        // 1. Add a large number of random elements
        List<OctElt> originalElements = new List<OctElt>();
        
        for (int i = 0; i < NUM_FUZZ_ELEMENTS; i++)
        {
            Vector3I pos = RandomPosition();
            OctElt element = new OctElt(i, pos);
            originalElements.Add(element);
            _sut = _sut?.AddElement(element);
        }
        
        // Verify all elements were added
        AssertThat(_sut).IsNotNull();
        AssertInt(_sut.Elements.Count).IsEqual(NUM_FUZZ_ELEMENTS);
        
        // 2. Perform multiple random searches and verify results
        for (int i = 0; i < 50; i++)
        {
            (Vector3I min, Vector3I max) = RandomSearchBox(_random.Next(500) + 50);
            
            var results = _sut?.FindElementsInBox(min, max);
            
            // Count elements from original list that should be in this box
            int expectedCount = 0;
            foreach (var originalElt in originalElements)
            {
                Vector3I pos = originalElt.position;
                
                if (pos.X >= min.X && pos.X <= max.X &&
                    pos.Y >= min.Y && pos.Y <= max.Y &&
                    pos.Z >= min.Z && pos.Z <= max.Z)
                {
                    expectedCount++;
                }
            }
            
            // Count actual results
            int actualCount = 0;
            if (results != null)
            {
                foreach (var _ in results)
                {
                    actualCount++;
                }
            }
            
            // Verify match
            AssertInt(actualCount).IsEqual(expectedCount);
        }
    }
    
    [TestCase]
    public void Test_FuzzRemove()
    {
        // 1. Add random elements
        List<OctElt> elements = new List<OctElt>();
        
        for (int i = 0; i < NUM_FUZZ_ELEMENTS; i++)
        {
            Vector3I pos = RandomPosition();
            OctElt element = new OctElt(i, pos);
            elements.Add(element);
            _sut = _sut?.AddElement(element);
        }
        
        // 2. Remove a random subset of elements
        HashSet<int> removedIds = new HashSet<int>();
        int numToRemove = NUM_FUZZ_ELEMENTS / 2;
        
        for (int i = 0; i < numToRemove; i++)
        {
            int idToRemove = _random.Next(NUM_FUZZ_ELEMENTS);
            
            // Skip if already removed
            if (removedIds.Contains(idToRemove))
            {
                i--;
                continue;
            }
            
            removedIds.Add(idToRemove);
            _sut = _sut?.RemoveElement(idToRemove);
        }
        
        // Verify removal
        AssertThat(_sut).IsNotNull();
        AssertInt(_sut.Elements.Count).IsEqual(NUM_FUZZ_ELEMENTS - removedIds.Count);
        
        // 3. Search and verify removed elements are not found
        Vector3I min = new Vector3I(-_boundsSize.X/2, -_boundsSize.Y/2, -_boundsSize.Z/2);
        Vector3I max = new Vector3I(_boundsSize.X/2, _boundsSize.Y/2, _boundsSize.Z/2);
        var results = _sut?.FindElementsInBox(min, max);
        
        HashSet<int> foundIds = new HashSet<int>();
        if (results != null)
        {
            foreach (var foundElement in results)
            {
                foundIds.Add(foundElement.id);
            }
        }
        
        // No removed IDs should be found
        foreach (int removedId in removedIds)
        {
            AssertBool(foundIds.Contains(removedId)).IsFalse();
        }
        
        // All non-removed IDs should be found
        for (int i = 0; i < NUM_FUZZ_ELEMENTS; i++)
        {
            if (!removedIds.Contains(i))
            {
                AssertBool(foundIds.Contains(i)).IsTrue();
            }
        }
    }
    
    [TestCase]
    public void Test_FuzzUpdate()
    {
        // 1. Add random elements
        for (int i = 0; i < NUM_FUZZ_ELEMENTS; i++)
        {
            Vector3I pos = RandomPosition(useFullRange: false); // Keep in a smaller range initially
            OctElt element = new OctElt(i, pos);
            _sut = _sut?.AddElement(element);
        }
        
        // 2. Update positions of a random subset
        HashSet<int> updatedIds = new HashSet<int>();
        Dictionary<int, Vector3I> newPositions = new Dictionary<int, Vector3I>();
        int numToUpdate = NUM_FUZZ_ELEMENTS / 2;
        
        for (int i = 0; i < numToUpdate; i++)
        {
            int idToUpdate = _random.Next(NUM_FUZZ_ELEMENTS);
            
            // Skip if already updated
            if (updatedIds.Contains(idToUpdate))
            {
                i--;
                continue;
            }
            
            updatedIds.Add(idToUpdate);
            
            // Move to a completely different quadrant
            Vector3I newPos = new Vector3I(
                (int)((_random.NextDouble() * 2 - 1) * _boundsSize.X / 3 + _boundsSize.X / 3), // Right third
                (int)((_random.NextDouble() * 2 - 1) * _boundsSize.Y / 3 + _boundsSize.Y / 3), // Top third
                (int)((_random.NextDouble() * 2 - 1) * _boundsSize.Z / 3 + _boundsSize.Z / 3)  // Front third
            );
            
            newPositions[idToUpdate] = newPos;
            _sut = _sut?.UpdateElement(idToUpdate, newPos);
        }
        
        // Verify update: Search in original area
        Vector3I oldMin = new Vector3I(-150, -150, -150);
        Vector3I oldMax = new Vector3I(150, 150, 150);
        var resultsOldArea = _sut?.FindElementsInBox(oldMin, oldMax);
        
        HashSet<int> foundInOldArea = new HashSet<int>();
        if (resultsOldArea != null)
        {
            foreach (var foundElement in resultsOldArea)
            {
                foundInOldArea.Add(foundElement.id);
            }
        }
        
        // 3. Search in new area and verify updated elements are found
        Vector3I newMin = new Vector3I(_boundsSize.X / 3 - 150, _boundsSize.Y / 3 - 150, _boundsSize.Z / 3 - 150);
        Vector3I newMax = new Vector3I(_boundsSize.X / 3 + 150, _boundsSize.Y / 3 + 150, _boundsSize.Z / 3 + 150);
        var resultsNewArea = _sut?.FindElementsInBox(newMin, newMax);
        
        HashSet<int> foundInNewArea = new HashSet<int>();
        if (resultsNewArea != null)
        {
            foreach (var foundElement in resultsNewArea)
            {
                foundInNewArea.Add(foundElement.id);
            }
        }
        
        // Debug output to help diagnose issues
        GD.Print($"Updated IDs count: {updatedIds.Count}");
        GD.Print($"Found in old area: {foundInOldArea.Count}");
        GD.Print($"Found in new area: {foundInNewArea.Count}");
        
        // Check for any overlapping IDs between updated and found in old area
        int overlappingCount = 0;
        foreach (int id in updatedIds)
        {
            if (foundInOldArea.Contains(id))
            {
                overlappingCount++;
                GD.Print($"Updated ID {id} still found in old area!");
            }
        }
        GD.Print($"Overlapping count: {overlappingCount}");
        
        // Verify updated elements are NOT in old area
        foreach (int updatedId in updatedIds)
        {
            AssertBool(foundInOldArea.Contains(updatedId)).IsFalse();
        }
        
        // Verify at least some updated elements are in new area
        // (Not all might be due to random positioning, but many should be)
        int foundCount = 0;
        foreach (int updatedId in updatedIds)
        {
            if (foundInNewArea.Contains(updatedId))
            {
                foundCount++;
            }
        }
        
        // At least 10% should be found in the new area
        AssertInt(foundCount).IsGreater(updatedIds.Count / 10);
    }
    
    [TestCase]
    public void Test_FuzzBoundaryPositions()
    {
        // Test elements positioned exactly at or near boundaries
        List<OctElt> boundaryElements = new List<OctElt>();
        int id = 0;
        
        // 1. Corners of the bounding box
        for (int x = -1; x <= 1; x += 2)
        {
            for (int y = -1; y <= 1; y += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    Vector3I cornerPos = new Vector3I(
                        x * _boundsSize.X / 2 - x, // Just inside boundary
                        y * _boundsSize.Y / 2 - y,
                        z * _boundsSize.Z / 2 - z
                    );
                    
                    OctElt elementCorner = new OctElt(id++, cornerPos);
                    boundaryElements.Add(elementCorner);
                    _sut = _sut?.AddElement(elementCorner);
                }
            }
        }
        
        // 2. Centers of faces
        Vector3I[] faceDirections = new Vector3I[]
        {
            new Vector3I(_boundsSize.X / 2 - 1, 0, 0),  // Right face
            new Vector3I(-_boundsSize.X / 2 + 1, 0, 0), // Left face
            new Vector3I(0, _boundsSize.Y / 2 - 1, 0),  // Top face
            new Vector3I(0, -_boundsSize.Y / 2 + 1, 0), // Bottom face
            new Vector3I(0, 0, _boundsSize.Z / 2 - 1),  // Front face
            new Vector3I(0, 0, -_boundsSize.Z / 2 + 1)  // Back face
        };
        
        foreach (Vector3I pos in faceDirections)
        {
            OctElt elementFace = new OctElt(id++, pos);
            boundaryElements.Add(elementFace);
            _sut = _sut?.AddElement(elementFace);
        }
        
        // 3. Center positions along boundary edges
        for (int x = -1; x <= 1; x += 2)
        {
            for (int y = -1; y <= 1; y += 2)
            {
                Vector3I edgePos = new Vector3I(
                    x * _boundsSize.X / 2 - x,
                    y * _boundsSize.Y / 2 - y,
                    0
                );
                
                OctElt elementEdge = new OctElt(id++, edgePos);
                boundaryElements.Add(elementEdge);
                _sut = _sut?.AddElement(elementEdge);
            }
        }
        
        for (int x = -1; x <= 1; x += 2)
        {
            for (int z = -1; z <= 1; z += 2)
            {
                Vector3I edgePos = new Vector3I(
                    x * _boundsSize.X / 2 - x,
                    0,
                    z * _boundsSize.Z / 2 - z
                );
                
                OctElt elementEdge = new OctElt(id++, edgePos);
                boundaryElements.Add(elementEdge);
                _sut = _sut?.AddElement(elementEdge);
            }
        }
        
        for (int y = -1; y <= 1; y += 2)
        {
            for (int z = -1; z <= 1; z += 2)
            {
                Vector3I edgePos = new Vector3I(
                    0,
                    y * _boundsSize.Y / 2 - y,
                    z * _boundsSize.Z / 2 - z
                );
                
                OctElt elementEdge = new OctElt(id++, edgePos);
                boundaryElements.Add(elementEdge);
                _sut = _sut?.AddElement(elementEdge);
            }
        }
        
        // Verify all were added correctly
        AssertThat(_sut).IsNotNull();
        AssertInt(_sut.Elements.Count).IsEqual(boundaryElements.Count);
        
        // Search for each boundary element individually to ensure it's found
        foreach (OctElt boundary in boundaryElements)
        {
            Vector3I pos = boundary.position;
            
            Vector3I searchMin = new Vector3I(
                pos.X - 1,
                pos.Y - 1,
                pos.Z - 1
            );
            
            Vector3I searchMax = new Vector3I(
                pos.X + 1,
                pos.Y + 1,
                pos.Z + 1
            );
            
            var results = _sut?.FindElementsInBox(searchMin, searchMax);
            
            bool found = false;
            if (results != null)
            {
                foreach (var result in results)
                {
                    if (result.id == boundary.id)
                    {
                        found = true;
                        break;
                    }
                }
            }
            
            // Each element should be found in its own tiny search box
            AssertBool(found).IsTrue();
        }
    }
    
    [TestCase]
    public void Test_FuzzPerformance()
    {
        // Test performance with lots of elements
        const int STRESS_ELEMENT_COUNT = 5000;
        
        // 1. Measure time to add elements
        long startAdd = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        
        // Add elements in small clusters to test node splitting
        for (int cluster = 0; cluster < 50; cluster++)
        {
            // Generate cluster center
            Vector3I clusterCenter = RandomPosition();
            
            // Add elements around this center
            for (int i = 0; i < STRESS_ELEMENT_COUNT / 50; i++)
            {
                // Random offset from cluster center
                Vector3I offset = new Vector3I(
                    (int)((_random.NextDouble() * 2 - 1) * 50),
                    (int)((_random.NextDouble() * 2 - 1) * 50),
                    (int)((_random.NextDouble() * 2 - 1) * 50)
                );
                
                Vector3I pos = new Vector3I(
                    clusterCenter.X + offset.X,
                    clusterCenter.Y + offset.Y,
                    clusterCenter.Z + offset.Z
                );
                OctElt element = new OctElt(cluster * 100 + i, pos);
                _sut = _sut?.AddElement(element);
            }
        }
        
        long endAdd = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        long addTime = endAdd - startAdd;
        
        // 2. Measure time for searches
        long startSearch = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        
        for (int i = 0; i < 20; i++)
        {
            (Vector3I min, Vector3I max) = RandomSearchBox(500);
            _sut?.FindElementsInBox(min, max);
        }
        
        long endSearch = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        long searchTime = endSearch - startSearch;
        
        // 3. Log performance metrics
        GD.Print($"OctTree Performance: Added {STRESS_ELEMENT_COUNT} elements in {addTime}ms");
        GD.Print($"OctTree Performance: 20 searches completed in {searchTime}ms");
        
        // No specific time assertion, but we should complete the test
        AssertThat(_sut).IsNotNull();
        AssertInt(_sut.Elements.Count).IsEqual(STRESS_ELEMENT_COUNT);
    }
}
