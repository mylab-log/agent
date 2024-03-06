namespace MyLab.LogAgent.Model
{
    public class DockerLabelName
    {
        public string Local { get; }
        public string? Namespace { get; }
        public string Full { get; }

        public DockerLabelName(string local, string? ns = null)
        {
            if (string.IsNullOrWhiteSpace(local))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(local));
            if (ns != null)
            {
                if(string.IsNullOrWhiteSpace(ns))
                    throw new ArgumentException("Value cannot be whitespace.", nameof(ns));
            }

            Local = local;
            Namespace = ns;
            Full = ns != null ? $"{ns}.{local}" : local;
        }

        public override string ToString()
        {
            return Full;
        }

        public static DockerLabelName Parse(string name)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));

            int nsSplitterPos = name.LastIndexOf('.');

            if (nsSplitterPos == name.Length - 1)
                throw new FormatException("Local name not found");

            return nsSplitterPos != -1
                ? new DockerLabelName(name.Substring(nsSplitterPos + 1), name.Remove(nsSplitterPos))
                : new DockerLabelName(name);
        }

        public static implicit operator DockerLabelName(string labelName)
        {
            return Parse(labelName);
        }

        public static implicit operator string(DockerLabelName labelName)
        {
            return labelName.ToString();
        }
    }
}
