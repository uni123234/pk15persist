using Project.Core.Text.Models;

namespace Project.Core.Text.Prototypes;

/// <summary>
/// [Creational Pattern: Prototype]
/// Registry storing pre-configured prototype templates of sections and document branches.
/// Clones registered prototypes on demand, eliminating repetitive manual tree assembly.
/// </summary>
public class SectionTemplateRegistry
{
    private readonly Dictionary<string, Heading> _templates = new(StringComparer.OrdinalIgnoreCase);

    public SectionTemplateRegistry()
    {
        SeedDefaultTemplates();
    }

    public void RegisterTemplate(string key, Heading template)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(template);
        _templates[key] = template;
    }

    /// <summary>
    /// Creates a cloned copy of the registered prototype section.
    /// </summary>
    public Heading CreateFromTemplate(string key, string? overrideName = null)
    {
        if (!_templates.TryGetValue(key, out var prototype))
        {
            throw new KeyNotFoundException($"Template prototype '{key}' not found in registry.");
        }

        var cloned = (Heading)prototype.Clone(deep: true);

        if (!string.IsNullOrWhiteSpace(overrideName))
        {
            return cloned with { Name = overrideName };
        }

        return cloned;
    }

    public bool HasTemplate(string key) => _templates.ContainsKey(key);

    public IEnumerable<string> TemplateKeys => _templates.Keys;

    private void SeedDefaultTemplates()
    {
        // 1. Standard Methodology Section Template
        var methodology = new Heading(10, "Methodology & Research", 1);
        methodology.AddChild(new Paragraph(11, "Standardized methodology description outlining research goals and steps."));
        methodology.AddChild(new TextLink(12, "Methodology Framework Reference", "https://example.org/methodology"));
        _templates["methodology"] = methodology;

        // 2. Standard Technical Architecture Section Template
        var architecture = new Heading(20, "Technical Architecture", 1);
        architecture.AddChild(new Paragraph(21, "Layered system architecture separating domain rules from infrastructural details."));
        architecture.AddChild(new TextImage(22, "System Architecture Diagram", "https://example.org/architecture.png"));
        _templates["architecture"] = architecture;

        // 3. Standard Conclusion Section Template
        var conclusion = new Heading(30, "Conclusion & Future Work", 1);
        conclusion.AddChild(new Paragraph(31, "Summary of verified outcomes and future scalability directions."));
        _templates["conclusion"] = conclusion;
    }
}
