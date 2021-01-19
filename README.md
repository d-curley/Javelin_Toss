# Javelin_Toss
Using inverse kinematics and computer vision, this interactive is a game much like dodgeball, 
where two players stand in front of a camera and screen holding a physical colored ball in their hand to control their digital hand in the game. 
From there, the goal is to catch the javelins and throw them at your opponent!

Objective: I absolutely loved Xbox Kinect games growing up; the physical play increased engagement and allowed for interesting interactions with friends. In my fascination with computer vision, I wanted to recreate those experiences with simple cameras, which are much less expensive than the infrared lenses used for the Xbox Kinect.

Description: With a computer vision (OpenCV) library imported into Unity, I focus on tracking colored objects and use their position in the frame to control the position of objects in the game. Since the program tracks only the users’ hands, I implement reverse kinematics to translate hand movement into the movement of a full arm. These games are designed to lean into the physical and visceral elements that the technology affords. 

For my scripts that run the game, see Assets/Scripts/Jav Dodgeball Scripts/


