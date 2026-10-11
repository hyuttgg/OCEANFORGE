using System;
using System.Collections.Generic;

namespace OceanForge.Contracts.AI;

public class AiDebateMessage
{
    public string AgentName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string BadgeColor { get; set; } = "#68C6C0";
    public string Perspective { get; set; } = string.Empty;
    public string KeyRecommendation { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class AiDebateSessionResult
{
    public string Topic { get; set; } = string.Empty;
    public string ConsensusVerdict { get; set; } = string.Empty;
    public List<AiDebateMessage> Debates { get; set; } = [];
    public DateTime FinishedAt { get; set; } = DateTime.UtcNow;
}
