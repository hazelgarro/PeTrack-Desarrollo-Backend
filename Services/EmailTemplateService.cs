namespace APIPetrack.Services
{
    public class EmailTemplateService
    {
        public string GetPasswordResetEmailBody(string resetUrl)
        {
            return $@"
            <!DOCTYPE html>
            <html lang='en'>
            <head>
                <meta charset='UTF-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <title>Reset Password</title>
            </head>
            <body style='font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 20px;'>
                <div style='max-width: 600px; margin: 0 auto; background-color: #ffffff; 
                            border-radius: 10px; overflow: hidden; 
                            box-shadow: 0 0 10px rgba(0, 0, 0, 0.1);'>
        
                    <div style='text-align: center; padding: 20px;'>       
                        <img src='https://raw.githubusercontent.com/hazelgarro/PeTrack-Desarrollo-Frontend/refs/heads/main/public/assets/img/PetrackTextWithLogo.svg' 
                             alt='Petrack Logo' 
                             style='max-width: 200px; height: auto;' />
                    </div>
        
                    <div style='padding: 20px; text-align: center;'>
                        <h2 style='color: #333333;'>Forgot Your Password?</h2>
                        <p style='color: #555555;'>Don't worry, just click the button below to reset it:</p>
                        <a href='{resetUrl}' 
                           style='display: inline-block; background-color: #045D5A; 
                                  color: #ffffff; text-decoration: none; padding: 15px 30px; 
                                  font-size: 16px; font-weight: bold; border-radius: 25px; 
                                  margin-top: 10px;'>Reset Password</a>
                    </div>

                    <div style='padding: 20px; text-align: center; font-size: 12px; color: #777777;'>
                        <p>If you did not request to reset your password, please ignore this email.</p>
                        <p>&copy; Petrack 2024. All rights reserved.</p>
                    </div>

                </div>
            </body>
            </html>";
        }
    }
}
