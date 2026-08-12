using NetTopologySuite.Geometries;

namespace MapProject.Entities;



public class Location
{
    public int Id { get; set; } 
    public string Name { get; set; } = string.Empty;
    public required Point Coordinates { get; set; }
    
    
    
    
    
}