Contexte :

L’idée n’est pas de donner des droits administrateurs à un utilisateur, mais de lui permettre de change l’heure système sans être administrateur
L’utilisation d’un service Windows permet d’isoler l’action sensible et d’exécuter le programme dans un contexte contrôlé et approuvé par l’IT et d’exposer uniquement une interface simple à l’utilisateur final.

Mise en service :

Installer le service Windows :
Lancer le script Install-Service.bat avec les droits administrateurs
Celui-ci se trouve dans le répertoire « Partie Services Windows »
Cela crée le service nommé « Time Change Service »
 

La partie interface utilisateur :

Le programme se trouve dans le répertoire « Partie Interface Utilisateur », le copier sur le disque de la machine à l’emplacement de votre choix (par exemple le dossier Utilisateur) et créer un raccourci vers celui-ci sur le bureau
Prérequis : Pour que l’interface du logiciel se lance, il faut que les redistribuables C++ soient installée


Fonctionnement :
Depuis l’interface utilisateur du programme client, ’utilisateur envoi une commande « Pipe » (c’est le mécanisme de communication inter-processus (IPC, Inter-Process Communication) qui permet à des applications et des services d'échanger des données.).
Quand le service système reçoit cet ordre, il exécute la tache de changement d’heure.
