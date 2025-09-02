namespace AIProjectOrchestrator.Domain.Configuration
{
    public class InstructionSettings
    {
        public const string SectionName = "InstructionSettings";
        
        public string InstructionsPath { get; set; } = "Instructions";
        public int CacheTimeoutMinutes { get; set; } = 5;
        public string[] RequiredSections { get; set; } = { "Role", "Task", "Constraints" };
        public int MinimumContentLength { get; set; } = 100;
    }
}