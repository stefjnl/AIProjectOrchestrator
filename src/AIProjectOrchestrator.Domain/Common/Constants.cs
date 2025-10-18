using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIProjectOrchestrator.Domain.Common
{
    /// <summary>
    /// Contains constant values used throughout the application to avoid magic strings.
    /// </summary>
    public static class ProjectConstants
    {
        /// <summary>
        /// Default project status value.
        /// </summary>
        public const string ActiveStatus = "active";
        
        /// <summary>
        /// Default project type value.
        /// </summary>
        public const string WebType = "web";
    }
    
    /// <summary>
    /// Contains AI-related constants used throughout the application.
    /// </summary>
    public static class AIConstants
    {
        /// <summary>
        /// Maximum context size in bytes before warning (~25K tokens).
        /// Prevents exceeding model token limits and excessive costs.
        /// </summary>
        public const int MaxContextSizeBytes = 100000;
        
        /// <summary>
        /// Minimum timeout for AI HTTP clients in seconds.
        /// </summary>
        public const int MinimumTimeoutSeconds = 120;
    }
    
    /// <summary>
    /// Contains entity type identifiers used for review entities.
    /// </summary>
    public static class EntityTypeConstants
    {
        /// <summary>
        /// Entity type identifier for requirements analysis.
        /// </summary>
        public const string RequirementsAnalysis = "requirementsanalysis";
        
        /// <summary>
        /// Entity type identifier for project planning.
        /// </summary>
        public const string ProjectPlanning = "projectplanning";
        
        /// <summary>
        /// Entity type identifier for story generation.
        /// </summary>
        public const string StoryGeneration = "storygeneration";
        
        /// <summary>
        /// Entity type identifier for prompt generation.
        /// </summary>
        public const string PromptGeneration = "promptgeneration";
    }
    
    /// <summary>
    /// Contains common property names used throughout the application.
    /// </summary>
    public static class PropertyNameConstants
    {
        /// <summary>
        /// Common property name for entity IDs.
        /// </summary>
        public const string Id = "Id";
        
        /// <summary>
        /// Property name for review IDs.
        /// </summary>
        public const string ReviewId = "ReviewId";
        
        /// <summary>
        /// Property name for status fields.
        /// </summary>
        public const string Status = "Status";
        
        /// <summary>
        /// Property name for type fields.
        /// </summary>
        public const string Type = "Type";
    }
}