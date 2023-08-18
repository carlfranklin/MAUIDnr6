# Building a Mobile Podcast Client App in MAUI Blazor Part 6

This app was shown on The .NET Show Episode #48: Publishing Apps Part 5

## Changes from the [last version](https://github.com/carlfranklin/MAUIDnr5) of the app:

### Architecture:

In this version, I addressed a major architectural issue, and fixed a few glitches that the previous architecture enabled.

The main issue was that the code for playing audio and keeping the state of it, was encapsulated in the code behind file *Details.razor.cs*. The problem is there can be multiple instances of this page, which can cause multiple episodes to play at the same time.

If you run the [last version](https://github.com/carlfranklin/MAUIDnr5) of the app, click on any episode's **Details** button, press **Play** to initiate a download, then press the **Back Arrow** button, then select another episode's **Details** button and press **Play**, both episodes will play at the same time.

This happens because the first episode Details page stays in memory.

The solution was to move all of that code to a state bag, that exists outside of the pages. 

I did have a static class called `AppState`, but it wasn't very clean.

So, I created a cascading component called `CascadingAppState` to replace it.

### MainLayout:

I added a NavMenu and the .NET Rocks! logo to the top of the Main Layout. 

The NavMenu currently lets you navigate between the Index page and the Playlists page

### Index Page:

I removed the Details button and instead handle a click or tap on the title to navigate to the Details page.

I modified the Search input to change the binding on every keystroke, rather than require you to click off of it. I also replaced the "Reset" button with a small circle x icon.

I added a button that shows the currently playing episode. When you click it, the audio stops.

Changed the text of "Next 20" button to "More". I added logic to not show the button if there are no more episodes available. I also changed the button text to "Loading..." while loading the next 20 episodes.

If there is no network access when you press the "More" button, a dialog pops up saying "You are not online."

If you search for a term that yields no results, I now display "No episodes match your filter"

Every description ended with a semicolon. I removed that.

If you are not online, you click on the title or description to show the details, and you have not downloaded the show details, a dialog pops up saying "You are not online."

The show description did not display correctly if it contained html markup.

I added a spinner to show when loading the initial set of shows.

When downloading shows in a playlist, I show a modal dialog (Blazored.Modal), which disallows access to the rest of the app. There is no cancel operation just yet.

I simplified the text in the playlist buttons so they all fit nicely on a small screen device.

### Playlists Page:

I removed the inline form for adding and editing a playlist name, and replaced it with a modal dialog (Blazored.Modal). If the playlist name already exists, the user is notified.

On navigation, I automatically select the first playlist if one is available. I also added a circle x button to deselect the current playlist.

I fixed a bug where you could add a playlist with the same name as another playlist. You could also edit an existing playlist name to be the same name as an existing one. Both bugs are fixed.

I replaced the HTML select element with a custom list Blazor component, which you can re-use in any MAUI (or web-based) Blazor app.

I increased the size of the up/down arrows used to re-order the episodes in a playlist.

### Details Page:

I moved the audio controls up on the page before the description, so you don't have to scroll down for them.

The description ended with a semicolon. I removed that.

The links were too close together, making it hard to select with a finger press. 

The show description and guest bio did not display correctly if it contained html markup.
