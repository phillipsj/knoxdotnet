#tool nuget:?package=Wyam&prerelease
#addin nuget:?package=Cake.Wyam&prerelease
#addin nuget:?package=Cake.Netlify
#addin nuget:?package=Cake.Npm
#addin nuget:?package=Cake.Powershell
//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Install-Netlify-Cli")
    .Does(()=> {
        var settings = new NpmInstallSettings();
        settings.Global = false;
        settings.LogLevel = NpmLogLevel.Warn;
        settings.AddPackage("netlify-cli");
        NpmInstall(settings);
    });


Task("Build")
    .Does(() => {
        Wyam(new WyamSettings {
            Recipe = "Blog",
            Theme = "CleanBlog",
            UpdatePackages = true
        });        
    });
    
Task("Preview")
    .Does(() => {
        Wyam(new WyamSettings {
            Recipe = "Blog",
            Theme = "CleanBlog",
            UpdatePackages = true,
            Preview = true,
            Watch = true
        });        
    });

Task("Debug")
    .Does(() => {
        StartProcess("../Wyam/src/clients/Wyam/bin/Debug/wyam.exe",
            "-a \"../Wyam/src/**/bin/Debug/*.dll\" -r \"blog -i\" -t \"../Wyam/themes/Blog/CleanBlog\" -p --attach");
    });

Task("Deploy")    
    .IsDependentOn("Build")
    .Does(() => {
        var token = EnvironmentVariable("NETLIFY_KNOXDOTNET");
        if(string.IsNullOrEmpty(token)) {
            throw new Exception("Could not get NETLIFY_KNOXDOTNET environment variable");
        }
        
        // Upload via curl and zip instead
        Zip("./output", "output.zip", "./output/**/*");
        StartProcess("curl", "--header \"Content-Type: application/zip\" --header \"Authorization: Bearer " + token + "\" --data-binary \"@output.zip\" --url https://api.netlify.com/api/v1/sites/phillipsj.netlify.com/deploys");
    });

Task("Netlify-Deploy")
    .IsDependentOn("Install-Netlify-Cli")
    .IsDependentOn("Build")
    .Does(() => {
        var token = EnvironmentVariable("NETLIFY_KNOXDOTNET");
        var siteId = EnvironmentVariable("NETLIFY_SITEID");
        if(string.IsNullOrEmpty(token)) {
            throw new Exception("Could not get NETLIFY_KNOXDOTNET environment variable");
        }
        if(string.IsNullOrEmpty(siteId)) {
            throw new Exception("Could not get NETLIFY_SITEID environment variable");
        }
        
        StartPowershellScript("./node_modules/.bin/netlify deploy", args => {
            args.Append("p", "output").Append("s", siteId).Append("t", token);
        });
    });

Task("Netlify-Addin-Deploy")
    .IsDependentOn("Install-Netlify-Cli")
    .IsDependentOn("Build")
    .Does(() => {
        var token = EnvironmentVariable("NETLIFY_KNOXDOTNET");
        var siteId = EnvironmentVariable("NETLIFY_SITEID");
        if(string.IsNullOrEmpty(token)) {
            throw new Exception("Could not get NETLIFY_KNOXDOTNET environment variable");
        }
        if(string.IsNullOrEmpty(siteId)) {
            throw new Exception("Could not get NETLIFY_SITEID environment variable");
        }
        
        NetlifyDeploy("output", siteId, token);        
    });
    
//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Default")
    .IsDependentOn("Preview");    
    
Task("AppVeyor")
    .IsDependentOn("Netlify-Addin-Deploy");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
