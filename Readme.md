# ArtNET.Config
This tool allows configuring ArtNET-devices on the network. It supports:
- Showing the status of the Node
- Changing the Name, ArtNET-Address & IP of a Node
- Manipulating port settings like RDM, Protocol, Delta-Mode, direction, ACN-Priority and disable Inputs (when supported)

For my own implementation for an USB-ArtNET Device it also supports a backup serial interface for when network traffic cannot be routed because of a broken configuration.

Please report any Issues and Bugs you find with this software. Please include your device and if possible a wireshark capture of the bug with the `artnet`-Filter active.

## Device overview
![Device Overview](/docs/overview.jpg)

This screen shows a list of all network devices on all network interfaces. To view a device, type the index. To refresh the list, type `r`. `Squawk!` shows whether any group inside the device has the `Squawking`-Flag set. Some Devices support this feature to find the Network device in the Software quickly by pressing a button on the physical device.

## Device Info
![Device Info Screen](/docs/deviceInfo.jpg)

The device info screen will show all Information given in the ArtPoll-Reply packets of a device. It will first list all the inputs and outputs of that device, sorted by their reply-group (A device can send multiple poll-replies to group ports together). A green entry means the port is currently alive and sending/receiving data. A red port means there was either an output short detected or input receive errors flagged.

From left to right the output stats show:
- The BindIndex of the group, used to select that specific group for more details
- The calculated address of the port
- The parts of the address split by network, subnet and universe
- The protocol, for most devices that will be DMX512 as the other stuff is legacy
- The Mode, either ArtNET or sACN with support for Continuous transmission (~) or Delta transmission (Δ)
- Whether RDM is enabled for that port or not
- The name of the group this port belongs to

For inputs:
- The BindIndex of the group
- Again the Address - calculated and in parts - and the protocol
- Whether this input is enabled or not
- And the name of the group

The Version group shows some Version info taken from the ArtPollReply-Packet.

The Status shows the three status flags in the reply. If multiple groups are shown, it will give a warning if the settings differ by group and only show the first group. If no warning is shown, all groups have the same status flags.
![Status differs](/docs/deviceInfo_status.jpg)

And lastly it will show any available Node-Reports of the groups, containing some device specific status messages.

To select a group, enter the ID of the group.

## Changing Group settings
Some settings are on a by-group basis. Those settings then apply to all the ports inside the group.

### Edit Name
Allows you to edit the Short Name and Long Name of a group. The current short and long name are shown at the top.
![Edit Name](/docs/editName.jpg)

### Edit ArtNet
Allows you to edit the ArtNET-Address of all ports in the group.
![Edit ArtNET](/docs/editArtNET.jpg)

There are two ways to edit the Address:
1. By using absolute values. In this case, you will enter a value between 0 and 32767 and the tool will calculate the Network, Subnet and Universe values for you. After your first entry, the range will be limited, as the Network and Subnet apply to all ports and thus limit the range of available values.
2. By setting the network, subnet and universe explicitly. In this case you can set all parts by hand and do the calculation yourself if neccessary.

### Edit ACN Priority
Changes the sACN-Priority. If two ports have the same address, the higher priority will win. Not relevant for ArtNET.

### Change LED State
You can toggle the LED state between Locate, Mute and Normal. Whether that actually does something depends on the device.

### Change Failover mode
You can change the failover-mode of the device between Hold, Zero, Full and Scene. Again, if that actually changes or does something depends on the device.
- Hold will keep the last DMX packet it received and continue sending it
- Zero will transmit all channels as `0` after a timeout
- Full will transmit all channels as `255` after a timeout
- Scene will transmit a custom scene that can be recorded after a timeout
- Record will store the current channel values for use in the `Scene`-Mode

### Serial commands
![Serial Commands](/docs/deviceInfo_serial.jpg)

If the device is detected to have an associated serial interface, some additional commands will be available:
- Power-Cycle device will issue a soft-reset and reboot the device
- Reboot the device into bootloader mode
- Reset the configuration to its defaults
- Configure the internal DHCP-Server
- Configure the static IP settings and mode

The serial interface is only meant to be used with [another project of mine](https://github.com/CShark/usb_dmx), so it won't show up in most cases. It is a kind of backup for when you brick your device and can't access it over the network anymore.

## Changing Port settings
![Port Settings](/docs/deviceInfo_port.jpg)

The port settings can be changed by typing the respective command. As all the port settings are only toggles, there is no special screen attached to those settings. If a setting is not supported to be changed it will be grayed out, but you can try to trigger it nonetheless. If the App thinks a setting is supported it may in fact be not. Just try to change it.

### Toggle RDM
On some devices, you can disable and enable RDM on output ports on a per-port basis. The current setting is shown in the Output overview. Triggering this command will try to toggle the RDM state using the respective ArtAddress-Command. On success, the indicator should change.

### Toggle Output Protocol
Some devices support both ArtNET and sACN. This toggle allows you to change the protocol on a per-port basis. The current protocol is shown in the Mode-Field of the Port. Triggering will try to switch the protocol using the respective ArtAddress-Command. If successful, the Mode-Field will change.

### Toggle Delta Mode
Some devices allow you to switch between Delta (Δ) and Continuous (~) transmission. Delta-Transmission will only transmit a DMX frame if a new ArtNET/sACN frame was sent. Triggering will try to switch using the ArtAddress-Commands and on success the indicator in the Mode-Field will change.

### Toggle Input Disabled
On ArtNET, Inputs may be disabled when they should not pollute the Network with unnecessary traffic. If a device supports this feature, a toggle will disable the input using an ArtInput-Packet. But there is no Status-Flag to indicate support for this feature, so it may or may not work.

### Toggle to input / output
Some devices allow the user to switch single ports between inputs and outputs. When triggered, the tool tries to change the port type using an ArtAddress-Command.