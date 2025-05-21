# 406JEM
To get started I used a blazor template for VS to spin this up quickly.. I wrote a few simple components, 'cause components.. used a blazor component library, 'cause blazor component. You get the idea.  I'll probably add more to the projects page once I get some urls wired up and working.

# API (Azure Function with an Http Trigger)
Code for this is contained to the 'ResumeFunctions' Project in the solution and the 'ResumeFunctions' folder in the solution folder. I started using a simple static asset and parsing to json to emulate an api, since I have written a simple Azure function (http trigger) to do this.  It still suses a static file to resolve my resume data, but probably will never promote this to an actual Azure service such as a relational or document db.  If I do I will probably just use cosmos to show the full integration.

# BLAZOR CLIENT
Code for this is contained to the 'BlazorClient' Project in the solution and the 'BlazorClient' folder in the solution folder. This was build off the VS Blazor template for static web assets.. umm.. it's written in Blazor and not exactly a complex webapp.. just a simple proof of ability. :)

# ANGULAR CLIENT
Code for this is contained to the 'AngularClient' Project in the solution and the 'AngularClient' folder in the solution folder. I built this with angular 17 after the fact just to POC my ability to work with Angular. It was build using angular 17 and I use the cli to generate the app, components, interfaces, everything I needed to the client app.

# CI/CD
the yaml for my github deploys are in the .github/workflows folder and I could the triggers or organize the actual yaml files differently and probably would on a more involved project.  Why I would organize it differently.. what I have here is not really optimized at all to limit build cycles.. I push new code.. it builds all the projects.. this could very easily be much more granular, but this is simply a POC as to the ability to leverage yaml for build/release activities.
