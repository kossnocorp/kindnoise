//
//  AppDelegate.swift
//  KindNoise
//
//  Created by Sasha Koss on 05.11.2022.
//

import Cocoa
import LaunchAtLogin

enum Sound : String {
    case rain, ocean, forest
}


@main
class AppDelegate: NSObject, NSApplicationDelegate {
    // App state
    var currentSound = Sound.rain
    var player : NSSound?
    var playing = false
    
    // Launch window
    @IBOutlet var launchWindow: NSWindow!
    @IBOutlet weak var feedbackLabel: NSTextField!
    @IBOutlet weak var showAtLaunchCheckbox: NSButton!
    
    // Status icon
    let statusItem = NSStatusBar.system.statusItem(withLength: NSStatusItem.variableLength)
    
    // Menu
    @IBOutlet weak var menu: NSMenu!
    @IBOutlet weak var togglePlayMenuItem: NSMenuItem!
    @IBOutlet weak var rainMenuItem: NSMenuItem!
    @IBOutlet weak var oceanMenuItem: NSMenuItem!
    @IBOutlet weak var forestMenuItem: NSMenuItem!
    @IBOutlet weak var launchAtLoginMenuItem: NSMenuItem!
    @IBOutlet weak var quitMenuItem: NSMenuItem!
    
    private var feedbackLabelText: NSMutableAttributedString {
      let string = NSMutableAttributedString(
        string: "If you have any feedback, please let me know.",
        attributes: [NSAttributedString.Key.foregroundColor: NSColor.labelColor, NSAttributedString.Key.font: NSFont.systemFont(ofSize: 16)]
      )
      string.addAttribute(.link, value: "https://kindnoise.com/feedback", range: NSRange(location: 33, length: 11))
      return string
    }

    func applicationDidFinishLaunching(_ aNotification: Notification) {
        // Handle launch window
        
        let showLaunchWindow = !UserDefaults.standard.bool(forKey: "hideLaunchWindow")
        showAtLaunchCheckbox.state = showLaunchWindow ? .on : .off
        if (showLaunchWindow) {
            launchWindow.makeKeyAndOrderFront(nil)
        }
        // Add feedback label
        feedbackLabel.attributedStringValue = feedbackLabelText

        if let button = statusItem.button {
            button.image = NSImage(named: "PausedIcon")
            button.action = #selector(self.menuIconClick(sender:))
            button.sendAction(on: [.leftMouseUp, .rightMouseUp])
        }
        
        setPlaying(false)
        
        // Load launch at login state
        updateLaunchAtLoginMenuItem()
        
        // Load current sound
        let currentSoundStr = UserDefaults.standard.string(forKey: "currentSound")
        setCurrentSound(currentSoundStr != nil ? Sound(rawValue: currentSoundStr!) ?? Sound.rain : Sound.rain)
    }

    func applicationWillTerminate(_ aNotification: Notification) {
        // Insert code here to tear down your application
    }

    func applicationSupportsSecureRestorableState(_ app: NSApplication) -> Bool {
        return true
    }
    
    // Methods
    
    func loadSound() {
        player = NSSound(named: NSSound.Name(currentSound.rawValue))
        player?.loops = true
    }
    
    func setCurrentSound(_ sound: Sound) {
        soundMenuItem().state = .off
        if (playing) {
            player?.stop()
        }
        
        currentSound = sound
        UserDefaults.standard.set(sound.rawValue, forKey: "currentSound")
        soundMenuItem().state = .on
        loadSound()
        if (playing) {
            player?.play()
        }
    }
    
    func soundMenuItem() -> NSMenuItem {
        switch currentSound {
        case .rain:
            return rainMenuItem
        case .ocean:
            return oceanMenuItem
        case .forest:
            return forestMenuItem
        }
    }
    
    func togglePlay() {
        setPlaying(!playing)
    }
    
    func updateLaunchAtLoginMenuItem() {
        launchAtLoginMenuItem.state = LaunchAtLogin.isEnabled ? .on : .off
    }
    
    func setPlaying(_ newPlaying: Bool) {
        if (newPlaying) {
            player?.play()
            playing = true
            togglePlayMenuItem.state = .on
            togglePlayMenuItem.title = "Pause"
            statusItem.button?.image = NSImage(named: "PlayingIcon")
        } else {
            player?.stop()
            playing = false
            togglePlayMenuItem.state = .off
            togglePlayMenuItem.title = "Play"
            statusItem.button?.image = NSImage(named: "PausedIcon")
        }
    }
    
    // Launch window handlers
    
    @IBAction func onShowAtLaunchToggle(_ sender: NSButton) {
        UserDefaults.standard.set(sender.state == .off, forKey: "hideLaunchWindow")
    }
    
    // Menu handlers
    
    @objc func menuIconClick(sender: NSStatusItem) {
        let event = NSApp.currentEvent!

        if event.type == NSEvent.EventType.leftMouseUp {
            statusItem.menu = menu
            statusItem.button?.performClick(nil)
            statusItem.menu = nil
        } else {
            togglePlay()
        }

    }
    
    @IBAction func handleTogglePlay(_ sender: NSMenuItem) {
        togglePlay()
    }
    
    @IBAction func switchToRain(_ sender: NSMenuItem) {
        setCurrentSound(Sound.rain)
    }
    
    @IBAction func switchToOcean(_ sender: NSMenuItem) {
        setCurrentSound(Sound.ocean)
    }
    
    
    @IBAction func switchToForest(_ sender: NSMenuItem) {
        setCurrentSound(Sound.forest)
    }
    
    @IBAction func toggleLaunchAtLogin(_ sender: Any) {
        LaunchAtLogin.isEnabled.toggle()
        updateLaunchAtLoginMenuItem()
    }
    
    
    @IBAction func showLaunchWindow(_ sender: NSMenuItem) {
        launchWindow.makeKeyAndOrderFront(nil)
    }
    
    
    @IBAction func quit(_ sender: NSMenuItem) {
        NSApp.terminate(nil)
    }
}

