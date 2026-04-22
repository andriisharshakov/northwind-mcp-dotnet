# AI-Assisted Quality Control for MCP Servers

## The Problem

When Claude operates on your data via MCP tools, things can go wrong:
- Claude misinterprets a vague prompt and modifies the wrong entity
- A tool returns ambiguous data and Claude makes a wrong inference
- Prompt injection causes unexpected tool calls
- Edge cases in tool logic produce incorrect results

Traditional testing catches bugs in code. But with AI, the failure mode is different —
the code works correctly, but Claude's *reasoning* was wrong.

## The Solution: Audit-Driven AI Review

Since we log every prompt → tool call → DB change chain via `correlation_id`,
we can feed these audit pairs back to Claude and ask it to spot anomalies.

```
┌─────────────────────────────────────────┐
│  audit_log (last N hours)               │
│  correlation_id | user_prompt | tool    │
│  tool_input | entity_type | new_values  │
└──────────────┬──────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────┐
│  AI Reviewer (Claude API)               │
│                                         │
│  Prompt:                                │
│  "Review these prompt/action pairs.     │
│   Flag any where the DB change seems    │
│   inconsistent with user intent."       │
└──────────────┬──────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────┐
│  Flagged pairs                          │
│  → Slack notification to reviewers      │
│  → Stored in review_queue table         │
└─────────────────────────────────────────┘
```

## What the Reviewer Looks For

| Pattern | Example |
|---------|---------|
| Wrong entity modified | Prompt: "update ALFKI price" → Product 42 was updated |
| Unexpected tool called | Prompt: "find customer" → `create_order` was called |
| Suspiciously large values | Prompt: "give discount" → price set to $0.01 |
| Write after read-only intent | Prompt: "show me orders" → DELETE was logged |
| Bulk operations from single prompt | 1 prompt → 50+ audit rows |

## Implementation Sketch

```csharp
public class McpAuditReviewer
{
    public async Task ReviewRecentSessionsAsync()
    {
        var sessions = await _db.AuditLog
            .Where(a => a.PerformedAt >= DateTime.UtcNow.AddHours(-6))
            .GroupBy(a => a.CorrelationId)
            .ToListAsync();

        foreach (var session in sessions)
        {
            var prompt = $"""
                Review this MCP session for anomalies.
                User prompt: "{session.First().UserPrompt}"
                Actions taken:
                {FormatActions(session)}

                Respond with JSON:
                {{
                  "suspicious": true/false,
                  "reason": "explanation if suspicious",
                  "severity": "low/medium/high"
                }}
                """;

            var response = await _claudeApi.CompleteAsync(prompt);
            var review = JsonSerializer.Deserialize<ReviewResult>(response);

            if (review.Suspicious && review.Severity != "low")
                await _notifier.AlertReviewersAsync(session, review);
        }
    }
}
```

## Review Queue Schema

```sql
CREATE TABLE review_queue (
    id              BIGSERIAL PRIMARY KEY,
    correlation_id  UUID NOT NULL,
    user_prompt     TEXT,
    flagged_reason  TEXT,
    severity        TEXT,   -- low / medium / high
    reviewed_by     TEXT,
    reviewed_at     TIMESTAMPTZ,
    resolution      TEXT,   -- false_positive / bug_found / prompt_improved
    created_at      TIMESTAMPTZ DEFAULT NOW()
);
```

## Rollout Strategy

**Phase 1 — Developer review**  
All flagged pairs go to the dev team. Goal: find tool logic bugs and prompt edge cases.

**Phase 2 — Manager review**  
High/medium severity go to team leads. Goal: catch user-facing issues.

**Phase 3 — Automated resolution**  
Low severity with known patterns auto-resolved. High severity still human-reviewed.

## Value Beyond Bug Catching

The audit review loop generates a dataset of:
- Prompts that worked well → use for prompt optimization
- Prompts that caused issues → use for guardrail improvements
- Common user intents → use to prioritize new tool development
