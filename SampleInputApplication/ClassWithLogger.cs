using System.Globalization;
using log4net;

namespace SampleInputApplication;

public class ClassWithLogger
{
    private static readonly ILog Logger = LogManager.GetLogger(typeof(ClassWithLogger));

    public bool AlwaysTrue()
    {
        if (Logger.IsInfoEnabled)
        {
            // Supported
            int kayttajienMaara = 5;
            bool titokannanNimi = false;
            
            Logger.Info("Tyhjennä kaikki tietokantaistunnot");
            Logger.Info($"{kayttajienMaara} käyttäjien poistaminen tietokannasta {titokannanNimi}");
            Logger.InfoFormat("Otetaan käyttöön {0} muutosta {1} käyttäjälle.", true, false);
            Logger.InfoFormat(CultureInfo.CurrentCulture, "Otetaan käyttöön {0} muutosta {1} käyttäjälle.", true, false);

            // Unsupported
            string logMsg = "A log message";
            Logger.Info(logMsg);
            Logger.Info("Ugly logging statement: " + true + " true" + false);
        }

        return true;
    }
}