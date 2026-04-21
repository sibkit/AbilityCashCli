namespace AbilityCashCli.Configuration;

public interface IConfigStore
{
    string ConfigPath { get; }
    AppConfig Load();
}
