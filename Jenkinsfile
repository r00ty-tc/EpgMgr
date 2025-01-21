def user_id
def group_id
node {
  user_id = sh(returnStdout: true, script: 'id -u').trim()
  group_id = sh(returnStdout: true, script: 'id -g').trim()
}

pipeline {
    agent {
        dockerfile {
            additionalBuildArgs '--build-arg USER_ID="' + user_id + '" --build-arg GROUP_ID="' + group_id + '"'
        }
    }
    stages {
        stage('Checkout SCM') {
            steps {
                cleanWs(deleteDirs: true, disableDeferredWipeout: true)
                checkout scm
            }
        }
        stage('Build') {
            steps {
                withCredentials([
                  file(credentialsId: 'EpgMgrPluginSnk', variable: 'PLUGIN_KEY'),
                  file(credentialsId: 'EpgMgrAppSnk', variable: 'APP_KEY')]){
                    sh 'chmod 755 *.sh'
                    sh 'cp \"${PLUGIN_KEY}\" EpgMgrPlugin.snk'
                    sh 'cp \"${APP_KEY}\" EpgMgrApp.snk'
                    sh './buildall.sh'
                }
            }
        }
    }
}
