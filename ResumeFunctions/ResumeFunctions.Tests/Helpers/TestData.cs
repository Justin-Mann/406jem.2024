namespace ResumeFunctions.Tests.Helpers;

public static class TestData
{
    public const string ResumeJson = """
        [
          {
            "Id": "1",
            "FName": "Jane",
            "MName": "Q",
            "LName": "Doe",
            "Position": "Software Engineer",
            "Subtitle": "C#, Azure",
            "SimpleGoal": "Build great software.",
            "LogoFile": "/img/logo.png",
            "Profile": ["Detail oriented", "Team player"],
            "Contact": [
              { "Type": 0, "DisplayValue": "555-1234" },
              { "Type": 2, "DisplayValue": "jane@example.com", "MailTo": "jane@example.com" }
            ],
            "Education": [
              { "Name": "State University", "Degree": true, "DegreeName": "B.S. Computer Science", "DegreeYear": "2015" }
            ],
            "WorkExperience": [
              {
                "CompanyName": "Acme Corp",
                "Position": "Developer",
                "StartDate": "2015",
                "EndDate": "Present",
                "BulletList": ["Built things", "Fixed bugs"],
                "Note": "Remote position"
              }
            ],
            "CustomSections": [
              { "Name": "Languages", "CustomItems": [{ "Value": "C#", "Type": 0 }] }
            ]
          }
        ]
        """;
}
